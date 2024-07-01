use anchor_lang::prelude::*;
use session_keys::{SessionError, SessionToken, session_auth_or, Session};

declare_id!("AuA9cdiqTjyniHJX96fUmGGnAHaQmk4YyVwgMAjRrRKS");

#[program]
pub mod khel_contracts 
{
    use super::*;

    pub fn initialize(ctx: Context<InitializePlayer>, username: String) -> Result<()> 
    {
        let player = &mut ctx.accounts.player;

        player.username = username;
        player.authority = ctx.accounts.authority.key();
        player.level = 0;

        Ok(())
    }

    #[session_auth_or(ctx.accounts.player_stats.authority.key() == ctx.accounts.player_stats.key(), GameErrorCode::WrongAuthority)]
    pub fn initialize_game(ctx: Context<InitializeGame>, _game_name: String) -> Result<()> 
    {
        let player_stats = &mut ctx.accounts.player_stats;

        player_stats.authority = ctx.accounts.player.authority.key();
        player_stats.highest_score = 0;
        player_stats.previous_score = 0;
        player_stats.times_played = 0;

        Ok(())
    }
}

#[derive(Accounts)]
#[instruction(username: String)]
pub struct InitializePlayer<'info>
{
    #[account(init, payer = authority, seeds = [b"Player", authority.key().as_ref()], bump, space = 8 + 32 + 2 + 4 + username.len())]
    pub player: Account<'info, Player>,

    #[account(mut)]
    pub authority: Signer<'info>,

    pub system_program: Program<'info, System>
}

#[derive(Accounts, Session)]
#[instruction(game_name: String)]
pub struct InitializeGame<'info>
{
    #[account(init, payer = signer, seeds = [b"PlayerGameStats", game_name.as_bytes(), player.authority.key().as_ref()], bump, space = 8 + 32 + 2 + 2 + 2)]
    pub player_stats: Account<'info, Stats>,

    #[account(seeds = [b"Player", player.authority.key().as_ref()], bump)]
    pub player: Account<'info, Player>,

    #[account(mut)]
    pub signer: Signer<'info>,

    #[session(signer = signer, authority = player.authority.key())]
    pub session_token: Option<Account<'info, SessionToken>>,

    pub system_program: Program<'info, System>
}

#[account]
pub struct Player
{
    pub authority: Pubkey,
    pub username: String,
    pub level: u16
}

#[account]
pub struct Stats
{
    pub authority: Pubkey,
    pub times_played: u16,
    pub previous_score: u16,
    pub highest_score: u16
}

#[error_code]
pub enum GameErrorCode 
{
    #[msg("Wrong Authority")]
    WrongAuthority,
}