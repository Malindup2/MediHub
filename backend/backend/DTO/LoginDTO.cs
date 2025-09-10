﻿
using System.ComponentModel.DataAnnotations;
namespace backend.DTO;
public class LoginDTO

{

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;
}
