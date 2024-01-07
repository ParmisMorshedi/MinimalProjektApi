using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MinimalProjektApi.Data;
using MinimalProjektApi.Models;
using System.Net;
using System.Text.Json.Serialization;
using System.Text.Json;
using System;
using System.Reflection.Emit;
using Microsoft.Extensions.Hosting;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Collections.Generic;

namespace MinimalProjektApi
{
    public class Program
    {
        //private static int PersonId;

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            string connectionString = builder.Configuration.GetConnectionString("ApplicationContext");
            builder.Services.AddDbContext<ApplicationContext>(opt =>
            {
                opt.UseSqlServer(connectionString);
            });

            var app = builder.Build();

            app.MapGet("/", () => "Hello World!");


            //Endpoint 1-Get all people in the system 
            app.MapGet("/Person", (ApplicationContext context) =>
            {
                return Results.Json(context.Persons);
            });

            //Endpoint 2-Get all InterestsPerson associated with a specific person
            app.MapGet("/Interest/{PersonId}", (ApplicationContext context, int PersonId) =>
            {
                // Query to retrieve person and associated interests
                var query = from person in context.Persons
                            join interestLink in context.InterestLinks on person.Id equals interestLink.PersonId
                            join interest in context.Interests on interestLink.InterestId equals interest.Id

                            where person.Id == PersonId
                            select new
                            {
                                PersonId = person.Id,
                                FirstName = person.FirstName,
                                LastName = person.LastName,
                                InterestId = interest.Id,
                                InterestTitle = interest.Title,
                                InterestDescription = interest.Description

                            };


                var result = query.ToList();
                return new JsonResult(result);
            });




            //Endpoint 3-Get all links associated with a specific person  
            app.MapGet("/Links/{PersonId}", (ApplicationContext context, int PersonId) =>
            {
                var links = context.InterestLinks
                    .Where(link => link.PersonId == PersonId)
                    .Select(link => new
                    {
                        PersonId = link.PersonId,
                        InterestId = link.InterestId,
                        Url = link.Url
                    })
                    .ToList();


                return Results.Json(links);
            });



            //Endpoint 4-Connect a person to a new interest
            app.MapPost("/connectPersonToInterest/{personId}/{interestId}", (ApplicationContext context, int personId, int interestId) =>
            {
                // Retrieve person and interest based on IDs
                var person = context.Persons.FirstOrDefault(p => p.Id == personId);
                var interest = context.Interests.FirstOrDefault(i => i.Id == interestId);


                // Check if person and interest exist
                if (person != null && interest != null)
                {
                    
                    if (person.Interests == null)
                    {
                        person.Interests = new List<Interest>();
                    }

                    // Add interest to the person's interests list and save changes
                    person.Interests.Add(interest);
                    context.SaveChanges();

                    return Results.StatusCode((int)HttpStatusCode.Created);
                }
                else
                {
                    return Results.BadRequest("Person or interest not found!");
                }


            });
           

            //5-Insert new links for a specific person and a specific interest 
            app.MapPost("/AddLink", (ApplicationContext context, InterestLink interestlink, int PersonId, int InterestId) =>
            {
                var person = context.InterestLinks.FirstOrDefault(p => p.Id == PersonId);
                var interest = context.InterestLinks.FirstOrDefault(i => i.Id == InterestId);
                var url = context.InterestLinks.FirstOrDefault(u => u.Url == interestlink.Url);

                if (person == person && interest == null && url == null)
                {
                    return Results.BadRequest("Url must not be null.");
                }
                context.InterestLinks.Add(interestlink);
                context.SaveChanges();
                return Results.Created($"/AddLink/{interestlink.Id}", interestlink);

            });

            // Extra challenge 1: Get detailed information about a specific person including interests and links
            app.MapGet("/PersonDetails/{PersonId}", (ApplicationContext context, int PersonId) =>
            {
                var query = from person in context.Persons
                            join interestLink in context.InterestLinks on person.Id equals interestLink.PersonId
                            join interest in context.Interests on interestLink.InterestId equals interest.Id
                             where person.Id == PersonId
                            select new
                            {
                                PersonId = person.Id,
                                FirstName = person.FirstName,
                                LastName = person.LastName,
                                InterestId = interest.Id,
                                InterestTitle = interest.Title,
                                InterestDescription = interest.Description,
                                Url = interestLink.Url,

                            };


                var result = query.ToList();

                return new JsonResult(result);
            });

            // Extra challenge 2: Search for persons based on a provided search string

            app.MapGet("/Persons", (ApplicationContext context, string search) =>
            {
                var query = from person in context.Persons
                            where person.FirstName.Contains(search) 
                            select new
                            {
                                PersonId = person.Id,
                                FirstName = person.FirstName,
                                LastName = person.LastName,
                                PhoneNumber = person.PhoneNumber,
                                Interests = person.Interests.Select(i => new
                                {
                                    InterestId = i.Id,
                                    InterestTitle = i.Title,
                                    InterestDescription = i.Description
                                }),
                                InterestLinks = person.InterestLinks.Select(il => new
                                {
                                    InterestLinkId = il.Id,
                                    Url = il.Url
                                })
                            };

                var result = query.ToList();

                return Results.Ok(result);
            });


            // Extra challenge 3: Paginate and retrieve a subset of persons based on search criteria
            app.MapGet("/persons/paginated", (ApplicationContext context, string search, int page = 1, int pageSize = 10) =>
            {
                var result = context.Persons
                    .Where(person =>  person.FirstName.Contains(search))
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(person => new
                    {
                        PersonId = person.Id,
                        FirstName = person.FirstName,
                        LastName = person.LastName,
                        PhoneNumber = person.PhoneNumber,
                        Interests = person.Interests.Select(i => new
                        {
                            InterestId = i.Id,
                            InterestTitle = i.Title,
                            InterestDescription = i.Description
                        }),
                        InterestLinks = person.InterestLinks.Select(il => new
                        {
                            InterestLinkId = il.Id,
                            Url = il.Url
                        })
                    })
                    .ToList();

                return Results.Ok(result);
            });
            app.Run();
        }
    }

}
