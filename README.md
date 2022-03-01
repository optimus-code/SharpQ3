# SharpQ3

![](https://img.shields.io/github/stars/optimus-code/SharpQ3.svg) ![](https://img.shields.io/github/forks/optimus-code/SharpQ3.svg) ![](https://img.shields.io/github/issues/optimus-code/SharpQ3.svg) [![GitHub contributors](https://img.shields.io/github/contributors/optimus-code/SharpQ3.svg)](https://GitHub.com/optimus-code/SharpQ3/graphs/contributors/) [![GitHub license](https://img.shields.io/github/license/optimus-code/SharpQ3.svg)](https://github.com/Naereen/StrapDown.js/blob/master/LICENSE)
 ![](https://img.shields.io/github/release/optimus-code/SharpQ3.svg) [![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg?style=flat-square)](http://makeapullrequest.com)
 
### Description 

SharpQ3 is a C# port of the Kenny Edition of Quake III Arena. This is a huge undertaking and far from complete. If you have C++ and C# experience and would like to get involved let me know! PRs are welcome!

### Conversion progress (Rough estimates)

Initial work is starting with code in QCommon as it is obviously the code that would be most useful to port at this point.

- [ ] CGame
	- [ ] cg_consolecmds.cs - 1%
	- [ ] cg_draw.cs - 1%
	- [ ] cg_drawtools.cs - 1%
	- [ ] cg_effects.cs - 1%
	- [ ] cg_ents.cs - 1%
	- [ ] cg_event.cs - 1%
	- [ ] cg_info.cs - 1%
	- [ ] cg_local.cs - 1%
	- [ ] cg_localents.cs - 1%
	- [ ] cg_main.cs - 1%
	- [ ] cg_marks.cs - 1%
	- [ ] cg_particles.cs - 1%
	- [ ] cg_players.cs - 1%
	- [ ] cg_playerstate.cs - 1%
	- [ ] cg_predict.cs - 1%
	- [ ] cg_public.cs - 1%
	- [ ] cg_scoreboard.cs - 1%
	- [ ] cg_servercmds.cs - 1%
	- [ ] cg_snapshot.cs - 1%
	- [ ] cg_syscalls.cs - 5%
	- [ ] cg_view.cs - 1%
	- [ ] cg_weapons.cs - 1%
	- [ ] tr_types.cs - 1%

- [ ] Game
	- [ ] ai_chat.cs - 1%
	- [ ] ai_cmd.cs - 1%
	- [ ] ai_dmnet.cs - 1%
	- [ ] ai_dmq3.cs - 1%
	- [ ] ai_main.cs - 1%
	- [ ] ai_team.cs - 1%
	- [ ] ai_vcmd.cs - 1%
	- [ ] be_aas.cs - 1%
	- [ ] be_ai_chat.cs - 1%
	- [ ] be_ai_goal.cs - 1%
	- [ ] be_ai_move.cs - 1%
	- [ ] be_ai_weap.cs - 1%
	- [ ] bg_lib.cs - 1%
	- [ ] bg_local.cs - 1%
	- [ ] bg_misc.cs - 1%
	- [ ] bg_pmove.cs - 1%
	- [ ] bg_public.cs - 1%
	- [ ] bg_slidemove.cs - 1%
	- [ ] botlib.cs - 1%
	- [ ] chars.cs - 1%
	- [ ] g_active.cs - 1%
	- [ ] g_arenas.cs - 1%
	- [ ] g_bot.cs - 1%
	- [ ] g_client.cs - 1%
	- [ ] g_cmds.cs - 1%
	- [ ] g_combat.cs - 1%
	- [ ] g_items.cs - 1%
	- [ ] g_local.cs - 1%
	- [ ] g_main.cs - 1%
	- [ ] g_mem.cs - 1%
	- [ ] g_misc.cs - 1%
	- [ ] g_missile.cs - 1%
	- [ ] g_mover.cs - 1%
	- [ ] g_public.cs - 1%
	- [ ] g_rankings.cs - 1%
	- [ ] g_session.cs - 1%
	- [ ] g_spawn.cs - 1%
	- [ ] g_svcmds.cs - 1%
	- [ ] g_syscalls.cs - 1%
	- [ ] g_target.cs - 1%
	- [ ] g_team.cs - 1%
	- [ ] g_trigger.cs - 1%
	- [ ] g_utils.cs - 1%
	- [ ] g_weapon.cs - 1%
	- [ ] inv.cs - 1%
	- [ ] match.cs - 1%
	- [ ] menudef.cs - 1%
	- [ ] q_math.cs - 1%
	- [ ] surfaceflags.cs - 1%
	- [ ] syn.cs - 1%
	
- [ ] Engine
	- [ ] QCommon 
		- [ ] cm_load.cs - 1%
		- [ ] cm_local.cs - 1%
		- [ ] cm_patch.cs - 1%
		- [ ] cm_polylib.cs - 1%
		- [ ] cm_test.cs - 1%
		- [ ] cm_trace.cs - 1%
		- [ ] cmd.cs - 53%
		- [ ] common.cs - 6%
		- [ ] cvar.cs - 5%
		- [ ] files.cs - 5%
		- [ ] huffman.cs - 1%
		- [ ] md4.cs - 1%
		- [ ] msg.cs - 1%
		- [ ] net_chan.cs - 1%
		- [ ] q_shared.cs - 46%
		- [ ] qcommon.cs - 95%
		- [ ] qfiles.cs - 95%
		- [ ] vm.cs - 1%
		- [ ] vm_interpreted.cs - 1%
		- [ ] vm_local.cs - 1%
		
	- [ ] Platform
		- [ ] win_gamma.cs - 1%
		- [ ] win_glimp.cs - 1%
		- [ ] win_input.cs - 1%
		- [ ] win_local.cs - 1%
		- [ ] win_main.cs - 1%
		- [ ] win_net.cs - 1%
		- [ ] win_qgl.cs - 1%
		- [ ] win_shared.cs - 1%
		- [ ] win_snd.cs - 1%
		- [ ] win_syscon.cs - 1%
		- [ ] win_wndproc.cs - 1%
	
	- [ ] Client
		- [ ] cl_cgame.cs - 1%
		- [ ] cl_cin.cs - 1%
		- [ ] cl_console.cs - 1%
		- [ ] cl_input.cs - 1%
		- [ ] cl_keys.cs - 1%
		- [ ] cl_main.cs - 1%
		- [ ] cl_net_chan.cs - 1%
		- [ ] cl_parse.cs - 1%
		- [ ] cl_scrn.cs - 1%
		- [ ] cl_ui.cs - 1%
		- [ ] client.cs - 1%
		- [ ] keys.cs - 1%
		- [ ] snd_adpcm.cs - 1%
		- [ ] snd_dma.cs - 1%
		- [ ] snd_local.cs - 1%
		- [ ] snd_mem.cs - 1%
		- [ ] snd_mix.cs - 1%
		- [ ] snd_public.cs - 1%
		- [ ] snd_wavelet.cs - 1%
		
	- [ ] Server
		- [ ] server.cs - 1%
		- [ ] sv_bot.cs - 1%
		- [ ] sv_ccmds.cs - 1%
		- [ ] sv_client.cs - 1%
		- [ ] sv_game.cs - 1%
		- [ ] sv_init.cs - 1%
		- [ ] sv_main.cs - 1%
		- [ ] sv_net_chan.cs - 1%
		- [ ] sv_snapshot.cs - 1%
		- [ ] sv_world.cs - 1%

	- [ ] Renderer
		- [ ] dx.cs - 1%
		- [ ] qgl.cs - 1%
		- [ ] tr_animation.cs - 1%
		- [ ] tr_backend.cs - 1%
		- [ ] tr_bsp.cs - 1%
		- [ ] tr_cmds.cs - 1%
		- [ ] tr_curve.cs - 1%
		- [ ] tr_font.cs - 1%
		- [ ] tr_image.cs - 1%
		- [ ] tr_init.cs - 1%
		- [ ] tr_light.cs - 1%
		- [ ] tr_local.cs - 1%
		- [ ] tr_main.cs - 1%
		- [ ] tr_marks.cs - 1%
		- [ ] tr_mesh.cs - 1%
		- [ ] tr_model.cs - 1%
		- [ ] tr_noise.cs - 1%
		- [ ] tr_public.cs - 1%
		- [ ] tr_scene.cs - 1%
		- [ ] tr_shade.cs - 1%
		- [ ] tr_shade_calc.cs - 1%
		- [ ] tr_shader.cs - 1%
		- [ ] tr_shadows.cs - 1%
		- [ ] tr_sky.cs - 1%
		- [ ] tr_surface.cs - 1%
		- [ ] tr_world.cs - 1%
		- [ ] vk.cs - 1%
		
	- [ ] BotLib
		- [ ] aasfile.cs - 1%
		- [ ] be_aas_bsp.cs - 1%
		- [ ] be_aas_bspq3.cs - 1%
		- [ ] be_aas_cluster.cs - 1%
		- [ ] be_aas_debug.cs - 1%
		- [ ] be_aas_def.cs - 1%
		- [ ] be_aas_entity.cs - 1%
		- [ ] be_aas_file.cs - 1%
		- [ ] be_aas_main.cs - 1%
		- [ ] be_aas_move.cs - 1%
		- [ ] be_aas_optimize.cs - 1%
		- [ ] be_aas_reach.cs - 1%
		- [ ] be_aas_route.cs - 1%
		- [ ] be_aas_routealt.cs - 1%
		- [ ] be_aas_sample.cs - 1%
		- [ ] be_ai_char.cs - 1%
		- [ ] be_ai_chat.cs - 1%
		- [ ] be_ai_gen.cs - 1%
		- [ ] be_ai_goal.cs - 1%
		- [ ] be_ai_move.cs - 1%
		- [ ] be_ai_weap.cs - 1%
		- [ ] be_ai_weight.cs - 1%
		- [ ] be_ea.cs - 1%
		- [ ] be_interface.cs - 1%
		- [ ] l_crc.cs - 1%
		- [ ] l_libvar.cs - 1%
		- [ ] l_log.cs - 1%
		- [ ] l_memory.cs - 1%
		- [ ] l_precomp.cs - 1%
		- [ ] l_script.cs - 1%
		- [ ] l_struct.cs	 - 1%	
		- [ ] l_utils.cs - 1%
		
- [ ] UI
	- [ ] keycodes.cs - 1%
	- [ ] ui_addbots.cs - 1%
	- [ ] ui_atoms.cs - 1%
	- [ ] ui_cinematics.cs - 1%
	- [ ] ui_confirm.cs - 1%
	- [ ] ui_connect.cs - 1%
	- [ ] ui_controls2.cs - 1%
	- [ ] ui_credits.cs - 1%
	- [ ] ui_demo2.cs - 1%
	- [ ] ui_display.cs - 1%
	- [ ] ui_gameinfo.cs - 1%
	- [ ] ui_ingame.cs - 1%
	- [ ] ui_local.cs - 1%
	- [ ] ui_login.cs - 1%
	- [ ] ui_main.cs - 1%
	- [ ] ui_menu.cs - 1%
	- [ ] ui_mfield.cs - 1%
	- [ ] ui_mods.cs - 1%
	- [ ] ui_network.cs - 1%
	- [ ] ui_options.cs - 1%
	- [ ] ui_playermodel.cs - 1%
	- [ ] ui_players.cs - 1%
	- [ ] ui_playersettings.cs - 1%
	- [ ] ui_preferences.cs - 1%
	- [ ] ui_public.cs - 1%
	- [ ] ui_qmenu.cs - 1%
	- [ ] ui_rankings.cs - 1%
	- [ ] ui_rankstatus.cs - 1%
	- [ ] ui_removebots.cs - 1%
	- [ ] ui_serverinfo.cs - 1%
	- [ ] ui_servers2.cs - 1%
	- [ ] ui_setup.cs - 1%
	- [ ] ui_signup.cs - 1%
	- [ ] ui_sound.cs - 1%
	- [ ] ui_sparena.cs - 1%
	- [ ] ui_specifyleague.cs - 1%
	- [ ] ui_specifyserver.cs - 1%
	- [ ] ui_splevel.cs - 1%
	- [ ] ui_sppostgame.cs - 1%
	- [ ] ui_spreset.cs - 1%
	- [ ] ui_spskill.cs - 1%
	- [ ] ui_startserver.cs - 1%
	- [ ] ui_syscalls.cs - 1%
	- [ ] ui_team.cs - 1%
	- [ ] ui_teamorders.cs - 1%
	- [ ] ui_video.cs - 1%
