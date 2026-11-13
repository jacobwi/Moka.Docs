namespace Moka.Docs.Themes.Default;

public static partial class EmbeddedThemeProvider
{
	private const string _headBootstrap = """

	                                          <script>document.documentElement.classList.remove('no-js');try{const d=document.documentElement,g=k=>{try{return localStorage.getItem(k)}catch(e){}return null};const s={{ if theme.style_selector }}g('mokadocs-style')||{{ end }}'{{ theme.style }}';d.setAttribute('data-style',s);const m=g('mokadocs-theme'),t=m||([{{ for option in theme.style_options }}{{ if option.dark_first }}'{{ option.name | html.escape | string.escape }}',{{ end }}{{ end }}].includes(s)||matchMedia('(prefers-color-scheme:dark)').matches?'dark':'light');d.setAttribute('data-theme',t);const c={{ if theme.color_themes != false }}g('mokadocs-color-theme')||{{ end }}'{{ theme.initial_color_theme | html.escape | string.escape }}';if(c)d.setAttribute('data-color-theme',c);const p=[{{ for code in theme.code_themes }}{{ if code.is_dark && code.counterpart != "" }}['{{ code.name | html.escape | string.escape }}','{{ code.counterpart | html.escape | string.escape }}'],{{ end }}{{ end }}],b=d.getAttribute('data-code-theme'),f=m&&p.find(x=>x.includes(b));d.setAttribute('data-code-theme',{{ if theme.code_theme_selector != false }}g('mokadocs-code-theme')||{{ end }}(f?f[m==='light'?1:0]:b));const cs={{ if theme.code_style_selector != false }}g('mokadocs-code-style')||{{ end }}'{{ theme.code_style | html.escape | string.escape }}';d.setAttribute('data-code-style',cs){{ if theme.text_size_selector != false }};const z=g('mokadocs-text-size');if(['90','112.5','125'].includes(z))d.setAttribute('data-text-size',z){{ end }}{{ if theme.width_selector != false }};const w=g('mokadocs-reader-width');if(['narrow','wide','wider','full'].includes(w))d.setAttribute('data-reader-width',w){{ end }}{{ if theme.layout_selector }};const l={ {{ for part in theme.layout_parts }}'{{ part.name | html.escape | string.escape }}':'{{ part.names | html.escape | string.escape }}',{{ end }} };for(const k in l){const v=g('mokadocs-layout-'+k);if(v&&l[k].split(' ').includes(v)){if(v==='default')d.removeAttribute('data-'+k);else d.setAttribute('data-'+k,v)}}{{ end }};}catch(e){}</script>

	                                      """;

	// Loads the stylesheet of a style the reader picked on an earlier page, when it isn't the site's. It sits right
	// after main.css: a link written with document.write is parser-inserted, so it holds back the first paint like
	// the static links, and it lands before the layout variants' stylesheet, which has to come after every style.
	// Styles never match together, so it may come before the site's own style. The script waits for main.css to
	// load (a script after a stylesheet always does), which a returning reader has cached.
	private const string _styleLoader = """
	                                    {{ if theme.style_selector && theme.style_files.size > 0 }}<script>try{const v={ {{ for file in theme.style_files }}'{{ file.name | html.escape | string.escape }}':'{{ file.version | html.escape | string.escape }}',{{ end }} },s=document.documentElement.getAttribute('data-style');if(s!=='{{ theme.style | html.escape | string.escape }}'&&v.hasOwnProperty(s))document.write('<link rel="stylesheet" href="{{ base_path | html.escape | string.escape }}/_theme/css/style-'+s+'.css?v='+v[s]+'" />')}catch(e){}</script>{{ end }}
	                                    """;

	private const string _appearanceMenu = """

	                                                      {{ if theme.show_dark_mode_toggle || theme.style_selector || theme.color_themes != false || theme.code_theme_selector != false || theme.code_style_selector != false || theme.text_size_selector != false || theme.width_selector != false || theme.layout_selector }}
	                                                      <div class="appearance js-only">
	                                                          <button class="appearance-trigger" type="button" aria-label="{{ t.appearance_title | html.escape }}" aria-haspopup="dialog" aria-expanded="false" aria-controls="appearancePanel">
	                                                              <svg aria-hidden="true" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><use href="{{ icon_sprite | html.escape }}#sliders"/></svg>
	                                                          </button>
	                                                          <div class="appearance-panel" id="appearancePanel" role="dialog" aria-labelledby="appearanceTitle" data-default-style="{{ theme.style | html.escape }}" data-default-color-theme="{{ theme.initial_color_theme | html.escape }}" data-default-code-theme="{{ theme.code_theme | html.escape }}" data-default-code-style="{{ theme.code_style | html.escape }}" hidden>
	                                                              <div class="appearance-head">
	                                                                  <h2 class="appearance-title" id="appearanceTitle">{{ t.appearance_title | html.escape }}</h2>
	                                                                  <button class="appearance-close" type="button" aria-label="{{ t.appearance_close | html.escape }}"><svg aria-hidden="true" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"><use href="{{ icon_sprite | html.escape }}#close"/></svg></button>
	                                                              </div>
	                                                              {{ if theme.show_dark_mode_toggle }}
	                                                              <div class="appearance-section">
	                                                                  <p class="appearance-label" id="appearanceModeLabel">{{ t.appearance_mode | html.escape }}</p>
	                                                                  <div class="appearance-options appearance-segments" role="radiogroup" aria-labelledby="appearanceModeLabel" data-appearance="mode">
	                                                                      <button class="appearance-option" type="button" role="radio" aria-checked="false" tabindex="-1" data-value="light"><svg aria-hidden="true" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"><use href="{{ icon_sprite | html.escape }}#sun-small"/></svg><span>{{ t.appearance_light | html.escape }}</span></button>
	                                                                      <button class="appearance-option" type="button" role="radio" aria-checked="false" tabindex="-1" data-value="dark"><svg aria-hidden="true" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><use href="{{ icon_sprite | html.escape }}#moon"/></svg><span>{{ t.appearance_dark | html.escape }}</span></button>
	                                                                      <button class="appearance-option" type="button" role="radio" aria-checked="false" tabindex="-1" data-value="system"><svg aria-hidden="true" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><use href="{{ icon_sprite | html.escape }}#monitor"/></svg><span>{{ t.appearance_system | html.escape }}</span></button>
	                                                                  </div>
	                                                              </div>
	                                                              {{ end }}
	                                                              {{ if theme.style_selector }}
	                                                              <div class="appearance-section">
	                                                                  <p class="appearance-label" id="appearanceStyleLabel">{{ t.appearance_style | html.escape }}</p>
	                                                                  <div class="appearance-options appearance-styles" role="radiogroup" aria-labelledby="appearanceStyleLabel" data-appearance="style">
	                                                                  </div>
	                                                              </div>
	                                                              {{ end }}
	                                                              {{ if theme.color_themes != false }}
	                                                              <div class="appearance-section">
	                                                                  <p class="appearance-label" id="appearanceColorThemeLabel">{{ t.appearance_color | html.escape }}</p>
	                                                                  <div class="appearance-options appearance-swatches" role="radiogroup" aria-labelledby="appearanceColorThemeLabel" data-appearance="color-theme">
	                                                                  </div>
	                                                              </div>
	                                                              {{ end }}
	                                                              {{ if theme.code_theme_selector != false }}
	                                                              <div class="appearance-section">
	                                                                  <p class="appearance-label" id="appearanceCodeThemeLabel">{{ t.appearance_code_theme | html.escape }}</p>
	                                                                  <div class="appearance-options appearance-code-themes" role="radiogroup" aria-labelledby="appearanceCodeThemeLabel" data-appearance="code-theme">
	                                                                      <span class="appearance-group-label">{{ t.appearance_dark | html.escape }}</span>
	                                                                      <span class="appearance-group-label">{{ t.appearance_light | html.escape }}</span>
	                                                                  </div>
	                                                              </div>
	                                                              {{ end }}
	                                                              {{ if theme.code_style_selector != false }}
	                                                              <div class="appearance-section">
	                                                                  <p class="appearance-label" id="appearanceCodeStyleLabel">{{ t.appearance_code_style | html.escape }}</p>
	                                                                  <div class="appearance-options appearance-segments" role="radiogroup" aria-labelledby="appearanceCodeStyleLabel" data-appearance="code-style">
	                                                                  </div>
	                                                              </div>
	                                                              {{ end }}
	                                                              {{ if theme.text_size_selector != false }}
	                                                              <div class="appearance-section">
	                                                                  <p class="appearance-label" id="appearanceTextSizeLabel">{{ t.appearance_text_size | html.escape }}</p>
	                                                                  <div class="appearance-options appearance-segments" role="radiogroup" aria-labelledby="appearanceTextSizeLabel" data-appearance="text-size">
	                                                                      <button class="appearance-option" type="button" role="radio" aria-checked="false" tabindex="-1" data-value="90">90%</button>
	                                                                      <button class="appearance-option" type="button" role="radio" aria-checked="false" tabindex="-1" data-value="100">100%</button>
	                                                                      <button class="appearance-option" type="button" role="radio" aria-checked="false" tabindex="-1" data-value="112.5">112.5%</button>
	                                                                      <button class="appearance-option" type="button" role="radio" aria-checked="false" tabindex="-1" data-value="125">125%</button>
	                                                                  </div>
	                                                              </div>
	                                                              {{ end }}
	                                                              {{ if theme.width_selector != false }}
	                                                              <div class="appearance-section">
	                                                                  <p class="appearance-label" id="appearanceReaderWidthLabel">{{ t.appearance_reading_width | html.escape }}</p>
	                                                                  <div class="appearance-options appearance-segments" role="radiogroup" aria-labelledby="appearanceReaderWidthLabel" data-appearance="reader-width">
	                                                                      <button class="appearance-option" type="button" role="radio" aria-checked="false" tabindex="-1" data-value="narrow">{{ t.appearance_width_narrow | html.escape }}</button>
	                                                                      <button class="appearance-option" type="button" role="radio" aria-checked="false" tabindex="-1" data-value="default">{{ t.appearance_width_default | html.escape }}</button>
	                                                                      <button class="appearance-option" type="button" role="radio" aria-checked="false" tabindex="-1" data-value="wide">{{ t.appearance_width_wide | html.escape }}</button>
	                                                                      <button class="appearance-option" type="button" role="radio" aria-checked="false" tabindex="-1" data-value="wider">{{ t.appearance_width_wider | html.escape }}</button>
	                                                                      <button class="appearance-option" type="button" role="radio" aria-checked="false" tabindex="-1" data-value="full">{{ t.appearance_width_full | html.escape }}</button>
	                                                                  </div>
	                                                              </div>
	                                                              {{ end }}
	                                                              {{ if theme.layout_selector }}
	                                                              <div class="appearance-section">
	                                                                  <p class="appearance-label" id="appearanceLayoutLabel">{{ t.appearance_layout | html.escape }}</p>
	                                                                  <div class="appearance-layout" role="group" aria-labelledby="appearanceLayoutLabel" data-default-layout="{{ for part in theme.layout_parts }}{{ if theme.layout[part.key] != "default" }}{{ part.name | html.escape }}={{ theme.layout[part.key] | html.escape }} {{ end }}{{ end }}">
	                                                                  </div>
	                                                              </div>
	                                                              {{ end }}
	                                                              <button class="appearance-reset" type="button">{{ t.appearance_reset | html.escape }}</button>
	                                                          </div>
	                                                      </div>
	                                                      {{ end }}

	                                      """;
}
