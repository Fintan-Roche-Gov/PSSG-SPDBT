using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text.Json.Nodes;

namespace Spd.Presentation.Screening.Swagger.ApiFilters
{
    /// <summary>
    /// To manually define the AddApplication Post API
    /// - Swashbuckle does not currently support multipart/form-data out of the box
    /// - Manually defined the multipart, file and Data model
    ///
    /// -- https://swagger.io/docs/specification/describing-request-body/multipart-requests/
    /// </summary>
    public class AddApplicationPostParamTypesFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation == null || context?.ApiDescription == null)
                return;

            var descriptor = context.ApiDescription.ActionDescriptor as ControllerActionDescriptor;
            if (descriptor == null ||
                !descriptor.ControllerName.Equals("Application") ||
                descriptor.ActionName != "AddApplication")
                return;

            try
            {
                // Initialize the request body if not already done
                if (operation.RequestBody == null)
                {
                    operation.RequestBody = new OpenApiRequestBody { Required = true };
                }

                // The Content property is read-only, so we must ensure it exists
                // and use indexer to add media types

                // Build the schema properties
                var properties = new Dictionary<string, IOpenApiSchema>
                {
                    {
                        "ConsentFormFile",
                        new OpenApiSchema
                        {
                            Type = JsonSchemaType.String,
                            Format = "binary",
                            Description = "PDF, Microsoft Word .docx/.doc files only"
                        }
                    },
                    {
                        "ApplicationCreateRequestJson",
                        new OpenApiSchema
                        {
                            Description = "See ApplicationCreateRequest schema",
                            Type = JsonSchemaType.Object,
                            Properties = new Dictionary<string, IOpenApiSchema>{
                                {
                                    "orgId",
                                    new OpenApiSchema
                                    {
                                        Type = JsonSchemaType.String,
                                        Format = "uuid"
                                    }
                                },
                                {
                                    "givenName",
                                    new OpenApiSchema
                                    {
                                        Type = JsonSchemaType.String | JsonSchemaType.Null,
                                    }
                                },
                                {
                                    "middleName1",
                                    new OpenApiSchema
                                    {
                                        Type = JsonSchemaType.String | JsonSchemaType.Null,
                                    }
                                },
                                {
                                    "middleName2",
                                    new OpenApiSchema
                                    {
                                        Type = JsonSchemaType.String | JsonSchemaType.Null,
                                    }
                                },
                                {
                                    "surname",
                                    new OpenApiSchema
                                    {
                                        Type = JsonSchemaType.String | JsonSchemaType.Null,
                                    }
                                },
                                {
                                    "emailAddress",
                                    new OpenApiSchema
                                    {
                                        Type = JsonSchemaType.String | JsonSchemaType.Null,
                                    }
                                },
                                {
                                    "jobTitle",
                                    new OpenApiSchema
                                    {
                                        Type = JsonSchemaType.String | JsonSchemaType.Null,
                                    }
                                },
                                {
                                    "genderCode",
                                    new OpenApiSchema
                                    {
                                        Type = JsonSchemaType.String | JsonSchemaType.Null,
                                    }
                                },
                                {
                                    "dateOfBirth",
                                    new OpenApiSchema
                                    {
                                        Type = JsonSchemaType.String | JsonSchemaType.Null,
                                        Format = "date-time",
                                    }
                                },
                                {
                                    "contractedCompanyName",
                                    new OpenApiSchema
                                    {
                                        Type = JsonSchemaType.String | JsonSchemaType.Null,
                                    }
                                },
                                {
                                    "phoneNumber",
                                    new OpenApiSchema
                                    {
                                        Type = JsonSchemaType.String | JsonSchemaType.Null,
                                    }
                                },
                                {
                                    "driversLicense",
                                    new OpenApiSchema
                                    {
                                        Type = JsonSchemaType.String | JsonSchemaType.Null,
                                    }
                                },
                                {
                                    "occupationCode",
                                    new OpenApiSchema
                                    {
                                        Type = JsonSchemaType.String | JsonSchemaType.Null,
                                    }
                                },
                                {
                                    "occupationDesc",
                                    new OpenApiSchema
                                    {
                                        Type = JsonSchemaType.String | JsonSchemaType.Null,
                                    }
                                },
                                {
                                    "workSiteCity",
                                    new OpenApiSchema
                                    {
                                        Type = JsonSchemaType.String | JsonSchemaType.Null,
                                    }
                                },
                                {
                                    "workSiteProvinceCode",
                                    new OpenApiSchema
                                    {
                                        Type = JsonSchemaType.String | JsonSchemaType.Null,
                                    }
                                },
                                {
                                    "workSiteCountryCode",
                                    new OpenApiSchema
                                    {
                                        Type = JsonSchemaType.String | JsonSchemaType.Null,
                                    }
                                },
                                {
                                    "workSitePostalCode",
                                    new OpenApiSchema
                                    {
                                        Type = JsonSchemaType.String | JsonSchemaType.Null,
                                    }
                                }
                            }
                        }
                    }
                };

                // Create the media type with the schema
                var mediaType = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Object,
                        Properties = properties
                    },
                    Encoding = new Dictionary<string, OpenApiEncoding>
                    {
                        {
                            "ConsentFormFile",
                            new OpenApiEncoding
                            {
                                ContentType = "application/pdf,application/msword,application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                            }
                        }
                    }
                };

                // Add the multipart/form-data media type to the request body
                // Using indexer since Content is read-only
                if (operation.RequestBody.Content != null)
                {
                    operation.RequestBody.Content["multipart/form-data"] = mediaType;
                }
            }
            catch (Exception ex)
            {
                // Silently fail to prevent blocking Swagger generation
                System.Diagnostics.Debug.WriteLine($"AddApplicationPostParamTypesFilter error: {ex.Message}");
            }
        }
    }
}
