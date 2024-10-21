using System.Runtime.Serialization;
using AuctionService.Data;
using AuctionService.Dtos;
using AuctionService.Entities;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Controllers;

[ApiController]
[Route("api/auctions")]
public class AuctionController : ControllerBase
{
    private readonly AuctionDbContext _context;
    private readonly IMapper _mapper;

    public AuctionController(AuctionDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<List<AuctionDto>>> GetAllAuctions(string date)
    {
        // List<Auction> auctions = await _context.Auctions.Include(a => a.Item).OrderBy(a => a.Item.Make).ToListAsync();
        // return _mapper.Map<List<AuctionDto>>(auctions);

        var query = _context.Auctions.OrderBy(a => a.Item.Make).AsQueryable();
        if (!string.IsNullOrEmpty(date))
        {
            query = query.Where(x => x.UpdatedAt.CompareTo(DateTime.Parse(date).ToUniversalTime()) > 0);
        }

        return await query.ProjectTo<AuctionDto>(_mapper.ConfigurationProvider).ToListAsync();
    }


    [HttpGet("{id}")]
    public async Task<ActionResult<AuctionDto>> GetAuctionById(Guid id)
    {
        Auction auction = await _context.Auctions.Include(a => a.Item).FirstOrDefaultAsync(a => a.Id == id);
        if (auction == null) return NotFound();
        return _mapper.Map<AuctionDto>(auction);
    }


    [HttpPost]
    public async Task<ActionResult<AuctionDto>> CreateAuction(CreateAuctionDto auctionDto)
    {
        var auction = _mapper.Map<Auction>(auctionDto);
        //TODO: add current user as seller
        auction.Seller = "test";
        _context.Auctions.Add(auction);
        var result = await _context.SaveChangesAsync() > 0;
        if (!result) return BadRequest("Could not save changes to the DB");

        return CreatedAtAction(nameof(GetAuctionById), new { auction.Id }, _mapper.Map<AuctionDto>(auction));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateAuction(Guid id, UpdateAuctionDto updateAuctionDto)
    {
        var auction = await _context.Auctions.Include(x => x.Item).FirstOrDefaultAsync(a => a.Id == id);
        if (auction == null) return NotFound();

        //TODO: check seller == username

        auction.Item.Make = updateAuctionDto.Make ?? auction.Item.Make;
        auction.Item.Model = updateAuctionDto.Model ?? auction.Item.Model;
        auction.Item.Year = updateAuctionDto.Year ?? auction.Item.Year;
        auction.Item.Color = updateAuctionDto.Color ?? auction.Item.Color;
        auction.Item.Mileage = updateAuctionDto.Mileage ?? auction.Item.Mileage;

        var result = await _context.SaveChangesAsync() > 0;

        if (result) return Ok();

        return BadRequest(result);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAuction(Guid id)
    {
        var auction = await _context.Auctions.FindAsync(id);
        if (auction == null) return NotFound();

        //TODO: check seller == username

        _context.Auctions.Remove(auction);
        var result = await _context.SaveChangesAsync() > 0;
        if (!result) return BadRequest("Could not update DB");
        return Ok();
    }
}