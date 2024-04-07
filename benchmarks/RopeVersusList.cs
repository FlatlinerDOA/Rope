using System;
using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Attributes;
using Rope;

namespace Benchmarks;

[MemoryDiagnoser]
public class RopeVersusList
{
    public static readonly char[] LoremIpsum = """
Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nunc quis elit felis. Aenean efficitur pellentesque eros, vel pretium metus tempor a. Pellentesque volutpat mauris mi, ut tempor quam pretium ut. Quisque varius luctus congue. Nullam rutrum ante non erat finibus consequat. Vivamus congue nisi eget metus elementum consequat. Praesent aliquam efficitur sem eu lobortis. Nunc interdum turpis vitae nulla egestas malesuada. Quisque eu sapien ornare, dignissim nisl nec, feugiat dui. Curabitur ullamcorper pulvinar dui, at tempus ligula pretium scelerisque. Suspendisse convallis sollicitudin risus, porttitor molestie felis condimentum nec. Cras egestas ante ac ex placerat, at volutpat dui sollicitudin.

Vestibulum tristique eu velit ac consectetur. Class aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos himenaeos. Quisque faucibus, ante vel venenatis pulvinar, nisl lectus vestibulum orci, ac porta urna augue at mi. Quisque interdum orci lorem, vitae dignissim turpis maximus vitae. Maecenas rhoncus nec libero ac tristique. Ut feugiat mauris id quam bibendum, a tempus diam ullamcorper. Duis rutrum a turpis a laoreet. Nunc non porttitor orci. Duis ornare sagittis dolor vel lacinia. In in elit est. Suspendisse sollicitudin posuere dignissim. Donec non elementum odio. Sed efficitur iaculis est eu mollis. Aliquam est libero, facilisis eu risus ut, volutpat euismod tellus. Duis dui felis, luctus ut neque ut, eleifend tempus mauris.

Suspendisse luctus, eros ac scelerisque fringilla, elit elit mollis ipsum, vel congue urna nulla nec urna. Phasellus id urna vel turpis facilisis finibus in et neque. Aenean libero purus, porta et ipsum vel, viverra maximus lorem. Ut consequat convallis pulvinar. Aliquam gravida dui sed posuere finibus. Nam nibh sem, lacinia et congue vestibulum, suscipit non ante. Suspendisse vestibulum augue eu tortor volutpat, a pharetra ante luctus. Donec in ultricies ex. Nam fermentum eu mi quis ullamcorper. Cras sed diam suscipit, lobortis ligula aliquam, dignissim leo.

Cras lacinia sodales gravida. Maecenas sit amet cursus neque. Duis scelerisque nibh quis euismod elementum. Vestibulum nulla dolor, sollicitudin a nibh id, ullamcorper tempor elit. Donec fringilla maximus quam, in consequat felis tincidunt id. Proin fringilla ipsum massa, quis convallis velit commodo ut. Donec dapibus consequat sem, in consectetur mi consequat a.

In sodales facilisis est vitae feugiat. Maecenas a rhoncus nulla. Ut dolor risus, pretium sed lacus a, efficitur semper ligula. Nullam in mattis libero. Duis dignissim ac metus eget molestie. Vestibulum cursus orci non nibh pharetra, at auctor nisl imperdiet. Quisque volutpat, odio bibendum sodales eleifend, ligula elit iaculis massa, facilisis fringilla velit purus sit amet ante. Maecenas non leo id velit congue condimentum. Duis vulputate, dolor ut placerat lacinia, metus tellus porttitor leo, quis varius nibh enim quis dolor. Ut ultricies finibus dolor vel iaculis. Donec blandit, tellus a tristique eleifend, justo leo accumsan neque, sed rutrum odio lectus at elit. Nulla nulla elit, vulputate consequat hendrerit ut, accumsan non lacus. Nam lectus ipsum, auctor ut magna consectetur, dictum mattis neque. Aliquam scelerisque, magna quis eleifend laoreet, metus enim viverra turpis, ut feugiat justo odio ut dolor. Phasellus aliquam erat ac ligula fermentum maximus. Maecenas commodo risus eget malesuada imperdiet.

Sed vestibulum sapien a pharetra auctor. Fusce imperdiet ultrices eros ac molestie. Praesent tristique risus et nulla commodo, sed efficitur arcu scelerisque. Maecenas sodales felis vel nunc tristique ultrices. Donec lacus leo, interdum ut fermentum a, facilisis a sem. Pellentesque sagittis diam quis quam lacinia pretium. Integer lobortis lacus tortor. Nulla dictum orci sit amet varius convallis.

Aenean vitae ligula consectetur, consectetur turpis et, fermentum arcu. Donec rhoncus eros velit, a pellentesque tortor fringilla ut. Sed eget pulvinar mi, at faucibus diam. Fusce ac mauris eget risus tincidunt vehicula ut non quam. Integer semper nibh ac augue malesuada condimentum. Vestibulum mattis, libero sit amet feugiat placerat, est arcu luctus orci, et vulputate leo massa quis nisl. Cras et tellus eu sapien consectetur maximus facilisis in mi. Nulla scelerisque felis eget nisi semper, eu sollicitudin nunc faucibus. Nulla id vestibulum nunc. Suspendisse interdum nunc nisi, ut mattis nisl feugiat hendrerit. Duis pellentesque ante eget dolor vulputate tempus. Vivamus congue tellus in justo dignissim, vel egestas urna fermentum. Fusce ultricies massa lacus, at ornare urna condimentum id. Nunc sapien ligula, molestie ac rhoncus sit amet, condimentum at ante.

Lorem ipsum dolor sit amet, consectetur adipiscing elit. Suspendisse vestibulum imperdiet lorem, at viverra lorem accumsan vitae. Sed a auctor mauris, vel tempor nulla. Donec ullamcorper, turpis a auctor tristique, orci nisi hendrerit ex, id elementum leo lectus at tortor. Pellentesque molestie interdum tortor, eget varius erat faucibus vitae. Donec at nibh vel leo ornare consectetur. Nam ultricies fermentum enim.

Morbi dictum sem eu nunc rhoncus dignissim. Sed et varius orci. Proin ex ex, sodales eget suscipit sed, euismod ut urna. Sed molestie magna ac lorem consectetur ultricies. Mauris eget urna eros. Aenean euismod id nunc a molestie. Aliquam eget mauris ut metus accumsan pellentesque. Fusce non augue fringilla, bibendum quam ut, imperdiet nunc. Vestibulum dapibus hendrerit blandit. Pellentesque et nisi sit amet augue vehicula fermentum. Sed at sapien non mi egestas commodo. Vestibulum et bibendum diam, sed fermentum eros. Ut consectetur gravida erat in scelerisque. Nulla aliquet rhoncus tempor. Etiam a massa leo.

Donec at sapien sed justo dictum pretium. Vestibulum eros ipsum, tempor sit amet sagittis a, sagittis viverra risus. Praesent condimentum enim nec lectus ullamcorper rutrum. Duis ut elit luctus, ornare urna sit amet, pulvinar est. Maecenas non arcu dolor. Suspendisse placerat gravida libero. Curabitur volutpat porta ipsum nec rutrum. Vivamus porttitor eget lorem ultricies volutpat.

Nunc ultrices magna nec dui venenatis, ac aliquam dolor vulputate. Donec ultrices est quis felis rutrum volutpat. Cras auctor ultricies ultrices. Aliquam erat volutpat. Sed eu lorem eleifend, bibendum augue ut, vestibulum dolor. Curabitur porta eros sed enim egestas pellentesque. Donec fringilla rhoncus sagittis. Integer volutpat risus tellus, vel euismod lorem ultricies porttitor. Fusce vestibulum vulputate leo ut sollicitudin. Suspendisse sit amet felis sollicitudin, vehicula turpis eu, varius risus. Nullam venenatis at sem eu vulputate. Proin convallis lacus ante, sed aliquet libero lobortis ut. Cras a imperdiet diam.

Nulla sed magna nunc. Phasellus ultricies fringilla velit gravida sodales. Ut tincidunt turpis quis mauris dapibus gravida. Integer venenatis arcu lectus, eu volutpat ipsum efficitur pellentesque. Mauris imperdiet venenatis velit sit amet pretium. Mauris nunc dolor, fermentum eu pulvinar id, dignissim ut urna. Nulla vehicula laoreet tellus, at commodo risus sodales ut. Donec id mi mattis, sodales ligula vel, sodales felis. Vivamus a massa orci. In hac habitasse platea dictumst. Donec porta dapibus turpis sed aliquam. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Aliquam posuere risus tellus, ac interdum magna pulvinar vitae. Suspendisse a suscipit justo.

Class aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos himenaeos. Integer aliquet, libero vel posuere bibendum, lectus felis feugiat tortor, quis pharetra nibh velit ut ipsum. Praesent aliquam suscipit tellus in lobortis. Donec id quam nisl. Class aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos himenaeos. Curabitur porta felis nunc, id porta ligula lacinia sed. In a rhoncus dui. Cras vel augue sed massa pharetra ornare. Integer eget libero volutpat, consectetur eros mattis, iaculis nisi. Donec ullamcorper arcu nisi, nec bibendum purus imperdiet vulputate. Ut pulvinar nisi vel turpis auctor, non vulputate eros rhoncus. Nulla facilisi. Etiam id blandit mi, et consectetur odio. Sed vitae dolor at augue condimentum placerat sed at lorem.

Nulla elementum cursus vestibulum. Vestibulum elementum libero quis metus vestibulum, vitae rhoncus risus consequat. Vivamus consequat ipsum quam, id viverra lorem tempor a. Donec sagittis libero eget pharetra ullamcorper. Nam eget metus non ante eleifend elementum sit amet sit amet nisi. Vestibulum sodales magna a quam placerat interdum. Etiam ultricies condimentum pharetra. Nullam porttitor rutrum mi, at tempus eros condimentum sed. Vivamus scelerisque nisi et ex placerat ullamcorper. Ut sed purus aliquam, condimentum tortor in, ornare lectus. Donec ex quam, porta eget convallis at, tempor sed metus.

Nulla turpis sem, rutrum tempor pretium id, rhoncus non odio. Aliquam lobortis cursus arcu, posuere convallis mauris efficitur sit amet. Nullam in molestie arcu. Nunc commodo dolor sed efficitur aliquam. Nam eu mauris tristique, condimentum dolor at, efficitur sem. Quisque lobortis auctor nibh vitae pretium. Fusce tortor sapien, dapibus sit amet dolor eget, pretium mattis magna. Donec suscipit odio dui. Mauris id est nibh. Maecenas vitae magna varius, posuere odio quis, feugiat nisi. Suspendisse auctor erat tortor, in molestie nibh faucibus vel. Sed et quam est.

Quisque vitae porta nisl. Integer pharetra rhoncus purus, nec ultrices justo fermentum quis. Proin quis lorem eu enim bibendum auctor non eget elit. Aliquam erat volutpat. Nulla finibus, elit in facilisis rutrum, lorem metus consequat purus, id imperdiet leo mi quis velit. Integer semper blandit neque. Aliquam erat volutpat. Maecenas finibus nisi in mauris elementum placerat. Nam sit amet tincidunt nisl, sed scelerisque dolor. Quisque laoreet tellus vel erat egestas, sed volutpat metus viverra. Donec a metus maximus, venenatis purus eu, ornare tellus. Nulla quis diam consequat, molestie nunc sit amet, tempor ante. Donec ac massa at eros viverra viverra quis non erat. Nullam at sapien feugiat enim dapibus egestas. Pellentesque tincidunt finibus nulla eget varius.

Ut erat urna, commodo ut ex id, imperdiet sodales sapien. Donec non nulla velit. Donec pretium aliquam interdum. Duis fermentum dapibus turpis quis posuere. Nulla vitae nunc non arcu viverra venenatis. Aliquam placerat ultrices suscipit. Cras eleifend consectetur condimentum. Praesent dictum facilisis orci, sit amet finibus eros imperdiet laoreet. Quisque viverra blandit odio, eu volutpat odio suscipit non. Pellentesque blandit sollicitudin purus, non finibus sem. Cras mauris leo, consequat eu volutpat at, hendrerit ac nisl. Curabitur iaculis arcu et justo suscipit, id iaculis augue molestie. Suspendisse imperdiet ultrices felis, a pulvinar libero hendrerit vel. Donec non est id ipsum commodo aliquet.

Nam vitae purus non enim iaculis hendrerit quis et nisi. Nullam et placerat massa, vitae bibendum nisi. Aliquam pulvinar porttitor suscipit. In viverra justo porttitor augue vestibulum dapibus. Nam rhoncus nibh eget felis laoreet ultricies. Phasellus dignissim diam et ornare tincidunt. Nulla vitae tempor ligula, tincidunt lacinia ligula. Fusce facilisis felis sit amet tincidunt congue. Praesent consequat porta purus, a viverra urna ultricies vitae. Curabitur varius massa at diam euismod, sodales varius elit dignissim. Nullam vehicula erat nec enim pellentesque pulvinar. Nunc in vestibulum lorem. Sed iaculis suscipit lectus posuere feugiat. Sed hendrerit leo vel ornare tincidunt. Mauris faucibus metus eget laoreet rhoncus. In non ultricies lorem, nec lobortis tellus.

Suspendisse potenti. Donec tincidunt blandit tellus, at vulputate turpis euismod fringilla. Fusce at fringilla sapien. Morbi laoreet quam id elementum tempor. Nunc in tortor porta, imperdiet nulla ut, elementum lacus. Mauris ipsum sem, dignissim suscipit ante et, dignissim fringilla neque. Donec eu dolor vel dui sagittis vestibulum eu eu ipsum. Phasellus at tellus vitae purus scelerisque auctor. Interdum et malesuada fames ac ante ipsum primis in faucibus.

Curabitur porta libero neque, eu vulputate mi malesuada pellentesque. Pellentesque ut suscipit nisl. Praesent dictum, turpis ut aliquam tempus, leo enim consectetur ante, sit amet condimentum erat erat eget nibh. Etiam ac posuere massa, at ultrices sem. Pellentesque non metus metus. Donec lacinia ac lacus ut rutrum. Pellentesque consectetur mattis sollicitudin. Phasellus fermentum, sem sit amet aliquam sagittis, enim elit rutrum quam, ut tincidunt diam elit sed nisi. Fusce egestas, sapien eu semper lacinia, nisl justo eleifend diam, sed varius massa turpis in metus. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Praesent non ex quis velit molestie accumsan. Integer in ipsum facilisis ipsum mattis ultrices.

In feugiat egestas tristique. Aenean sagittis tellus nisl, sed placerat ligula vehicula sit amet. Morbi aliquam elit auctor ex suscipit ornare. Suspendisse vestibulum orci sed mauris tempor aliquet. Aliquam ornare massa nibh, at suscipit purus rhoncus ultricies. Pellentesque luctus ex ipsum, ut condimentum dui suscipit in. In malesuada, mi vel faucibus malesuada, purus nibh scelerisque mi, ut fermentum felis nisl id quam. Suspendisse accumsan sed nulla id laoreet. Ut cursus a quam eu sagittis. Suspendisse dignissim, odio ac facilisis eleifend, orci neque malesuada lectus, sit amet sagittis quam dui eu arcu. Morbi vitae bibendum metus. Sed lacinia erat in bibendum mattis. Vivamus et dictum libero.

Integer nec finibus felis, id aliquam ex. Sed cursus varius lectus. Nullam sapien ex, interdum ac neque congue, viverra porta erat. Proin pulvinar luctus ultrices. Vivamus dapibus sapien eu dui viverra lobortis. Morbi pharetra viverra metus, non porta ligula tincidunt nec. Nulla ipsum metus, elementum eget sem id, pharetra sodales lectus. Mauris eget nisl nibh. Phasellus tellus lacus, vehicula quis nisi eu, lobortis rhoncus justo. Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia curae; Morbi nec orci aliquam, elementum purus sit amet, sollicitudin massa. Nam congue urna eros, a interdum nunc lacinia vel. Duis eget quam tincidunt, placerat mi non, suscipit ante. Phasellus mattis suscipit est, sed dapibus nisi gravida in. Mauris consectetur ex vel odio commodo, quis sodales est vehicula.

Curabitur vulputate lacinia convallis. Nulla at quam ante. Ut quis tellus ac risus lacinia iaculis. Maecenas tristique arcu vel porta congue. Sed lorem felis, posuere sed tortor vitae, finibus molestie arcu. Sed sit amet tortor non nunc lobortis aliquam. Curabitur vel diam non nisi fringilla hendrerit. Aliquam malesuada lectus vel nulla pretium gravida.

Phasellus non dui elit. Sed sit amet lorem erat. Fusce egestas aliquam lorem, sed volutpat libero laoreet vitae. Nam quis dolor nunc. Aenean vulputate sodales eros vitae consectetur. Cras sit amet lacinia ligula, nec mollis odio. Aenean consequat, lacus vel auctor sodales, nibh sapien gravida enim, vitae placerat enim felis vel nulla. Vestibulum molestie massa a facilisis pellentesque. Class aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos himenaeos. Phasellus vulputate ut est porta sodales. Nulla ultricies, tellus eu ultricies posuere, orci ipsum pellentesque magna, vel imperdiet mi lectus sit amet tellus.

Nullam ultricies magna molestie, facilisis odio in, dignissim est. Nulla lacinia nibh eget magna gravida vulputate. Pellentesque pellentesque eu odio ut rhoncus. Proin tempor arcu quis pulvinar suscipit. Vivamus aliquet faucibus eleifend. Mauris non urna non tortor porta vehicula maximus nec arcu. Morbi eu nibh elit.

Lorem ipsum dolor sit amet, consectetur adipiscing elit. Vestibulum vel condimentum odio. Integer faucibus elit nibh, quis sagittis odio venenatis cursus. Cras nibh urna, scelerisque ut nulla eu, congue vehicula dui. Donec quis bibendum lorem. Donec feugiat erat et augue aliquam convallis. Cras porta tristique vestibulum. Nam augue leo, molestie at pellentesque ut, tempus vel est.

Phasellus id elit mollis, porttitor mi at, placerat elit. Duis tristique in tortor at congue. Donec sapien purus, condimentum a odio et, lobortis pretium lacus. Phasellus a lacus bibendum, sodales erat vitae, faucibus dui. Curabitur interdum consequat nisi, sed pretium sem. Maecenas tempor dictum imperdiet. Nam efficitur urna sit amet cursus lacinia. Donec magna diam, molestie eget tincidunt in, interdum sed dui. Sed eu urna justo. Pellentesque et lacus ultrices, lacinia massa vel, porttitor lorem.

Sed a dolor est. Donec tristique, dui in pharetra mollis, eros ante feugiat quam, et semper felis dui eget nisl. Interdum et malesuada fames ac ante ipsum primis in faucibus. Sed pharetra fermentum nisl, at tristique quam feugiat non. Pellentesque lobortis augue felis, in vulputate urna rhoncus vel. Nullam euismod odio quis risus rhoncus commodo. Proin at tortor sit amet felis porttitor iaculis a sit amet tellus. Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia curae; Nunc vel mi dapibus, eleifend ante in, placerat justo. Duis nec finibus nibh, nec sodales dolor. Donec tincidunt, ipsum at dignissim fringilla, risus purus varius lacus, vitae faucibus metus orci ut lacus. Nullam condimentum eu lacus non lacinia. Nam nec urna nec magna bibendum gravida.

Pellentesque sed ullamcorper massa. Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia curae; Nulla pharetra non tellus sed fringilla. Pellentesque eget risus mi. Donec in tellus metus. Morbi eleifend ornare justo. Suspendisse sollicitudin aliquet aliquet. Pellentesque a nunc nisl. Quisque commodo dictum metus, quis cursus odio porttitor dignissim. Nunc in finibus sapien. Praesent justo nisl, fermentum fermentum mauris sed, auctor vestibulum ex. Mauris pulvinar, neque ac mattis faucibus, orci nisi egestas orci, semper vulputate odio orci eget nulla.

Duis pellentesque dictum dolor, sit amet sagittis est molestie et. Donec luctus diam ac erat aliquam luctus. Curabitur maximus leo sapien, vitae dignissim massa tincidunt id. Mauris et ultricies diam, maximus placerat ligula. Sed viverra gravida nisi id blandit. Vivamus volutpat vulputate tellus, nec consequat lacus. Pellentesque purus sapien, gravida at lobortis non, vestibulum sit amet nulla. Vestibulum ex quam, venenatis vel sem tristique, accumsan pulvinar turpis. Etiam dui est, dapibus eget sollicitudin in, iaculis quis enim. Vivamus pulvinar, turpis sed vestibulum luctus, arcu eros euismod dui, vitae tristique augue neque sit amet tellus. Donec rhoncus maximus urna id vehicula.

Donec ac massa faucibus, vulputate ex vehicula, pretium ipsum. Vivamus congue lacinia luctus. Donec ut massa at sem efficitur varius a nec dui. Quisque volutpat velit suscipit sapien elementum pharetra. Integer semper libero quis metus dictum, id egestas nibh scelerisque. Donec nunc quam, dictum sed pulvinar a, fringilla id dui. Morbi fermentum nisi sit amet metus fermentum dignissim. Integer facilisis leo leo, nec tincidunt nisl accumsan vel. Curabitur neque ipsum, tincidunt sed gravida in, dignissim a quam. Morbi ut sapien sed sem imperdiet porta quis id tortor. Proin imperdiet odio sit amet lectus sodales scelerisque. Donec dapibus sed lectus nec fringilla. Phasellus eu congue nunc. Ut sed vestibulum risus.

Proin sodales facilisis dui, eget egestas magna consectetur non. Morbi at porttitor libero. Aliquam ullamcorper sagittis tristique. Vestibulum congue dignissim interdum. Nam ultrices, massa non finibus ultrices, quam dui elementum nulla, quis sodales nulla orci ac massa. Nulla condimentum luctus nulla non viverra. Pellentesque ac aliquet turpis.

Praesent blandit, est a ullamcorper congue, dolor enim semper nibh, sed efficitur massa dolor sit amet augue. Morbi aliquam non mauris quis ornare. Aenean euismod dignissim diam, a ornare urna vehicula in. Ut nec nisl ac purus consequat porttitor. Aliquam dictum risus erat, in commodo enim rutrum vel. Nam imperdiet dignissim augue, sed volutpat metus pulvinar sit amet. Aenean feugiat consequat nibh, et eleifend arcu interdum sed. Sed viverra posuere finibus. Praesent urna leo, ultrices in erat eu, sagittis consectetur libero. Proin sapien ligula, volutpat maximus leo nec, tincidunt vulputate ex. Donec pretium cursus turpis in faucibus. Nam eleifend quam vitae dui volutpat rutrum. Quisque commodo, erat id cursus consectetur, mauris elit posuere odio, et condimentum mauris elit eu enim. Nulla sagittis, turpis nec dignissim ultrices, enim nunc faucibus massa, non malesuada metus tortor at neque. In ex justo, viverra quis efficitur sit amet, convallis vulputate nunc. Cras in diam viverra, tincidunt urna a, suscipit tortor.

Donec maximus mauris nec justo fermentum, at viverra mauris volutpat. Suspendisse mattis risus euismod, euismod sem sit amet, placerat enim. Integer eget massa tincidunt, dignissim lectus vel, finibus sapien. Donec sem est, ultricies vel congue eu, molestie non justo. Class aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos himenaeos. Donec sit amet venenatis lorem, at luctus justo. Donec sit amet massa sem. Sed non egestas lectus. Sed eu dolor in tellus ornare ultricies. Proin et ullamcorper orci, non mattis massa.

In consequat ligula in est congue, at consectetur velit feugiat. Morbi ultrices magna vestibulum enim scelerisque placerat. Praesent dignissim ac nunc at dictum. Suspendisse auctor rutrum est nec congue. Donec vel sollicitudin dui, consequat vulputate ligula. Praesent libero magna, pharetra sit amet convallis at, molestie ac turpis. Maecenas sit amet turpis eget purus euismod eleifend vitae ut est. Nam eget augue nisi. Curabitur augue enim, mattis et fringilla sit amet, efficitur in arcu. Pellentesque ultricies ante egestas efficitur consectetur. Integer gravida, eros mollis fringilla consequat, massa metus condimentum enim, nec venenatis ipsum turpis at ligula. Maecenas euismod tempor blandit. Nullam efficitur sem sit amet consectetur egestas. Sed tristique in ex sit amet molestie.

Nunc tortor leo, sagittis ac justo at, ullamcorper ornare felis. Nam ut vehicula nunc, condimentum dignissim justo. Quisque imperdiet magna id porttitor consectetur. Nam vestibulum, odio non aliquet pharetra, quam erat tempus elit, ut bibendum odio nulla at velit. Sed imperdiet rhoncus ultrices. Proin ornare magna felis, dignissim mollis nisi sollicitudin a. Curabitur aliquet, odio ut pretium lobortis, nibh orci consectetur lorem, vel cursus arcu nulla sed diam. Quisque sollicitudin lorem massa. Sed viverra turpis nec vulputate tincidunt. Proin convallis ipsum sed ante rutrum, sed mollis enim tincidunt. Vestibulum et ipsum eget nisl tempus semper. Sed id lectus orci. Sed gravida magna et nibh facilisis euismod. Phasellus mattis vehicula eros, quis ultricies nunc porttitor vel. Phasellus ut tempor enim.

Fusce et aliquam velit, vel venenatis risus. Phasellus a egestas erat, sed ullamcorper ex. Sed auctor, nisl eget bibendum vulputate, nisl diam efficitur neque, quis bibendum magna neque vel dui. Donec sed tempor augue, sed pretium odio. Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia curae; Nam viverra sagittis magna vitae bibendum. Cras faucibus luctus lacus egestas interdum. Sed gravida, risus vel fringilla tristique, erat ligula facilisis lorem, a sollicitudin magna lacus vitae tellus. Sed eget dui sed lectus consectetur placerat. Etiam facilisis urna enim, nec dapibus ligula tincidunt quis. Aenean imperdiet vulputate ipsum, quis maximus leo pellentesque ut. Mauris volutpat velit nec nisi ornare mattis. Proin rutrum viverra elit ut dapibus. Quisque ac dolor vel orci posuere tempor.

In scelerisque ac tortor ac fringilla. Sed finibus odio in cursus commodo. Nulla in consectetur tortor. Praesent imperdiet, est at accumsan dictum, nisi leo tempus erat, et imperdiet dui sapien malesuada velit. Maecenas euismod pellentesque lorem, eu posuere nulla dictum sit amet. Nulla magna est, congue in dui sit amet, luctus luctus lectus. Suspendisse in ipsum ex. Proin imperdiet convallis suscipit. Ut varius pulvinar dolor, non vulputate neque semper vitae. Suspendisse sagittis vulputate nulla id vehicula. Nunc eu convallis lacus. Ut et bibendum neque. Praesent vel varius risus.

Nullam nisi ipsum, consectetur ut enim nec, semper pretium justo. Suspendisse ac tortor luctus, elementum ex et, pulvinar velit. Nunc sed ligula pharetra, sodales urna quis, placerat mi. Morbi vel sagittis purus. Maecenas gravida augue vel nisi tempus, sit amet molestie metus venenatis. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Aenean urna velit, scelerisque nec congue non, suscipit vel eros. Mauris egestas ornare urna, quis laoreet turpis fringilla eu. Nunc tempor eget felis non vestibulum. Integer ac nibh sem. Donec id metus semper nisl dignissim finibus at eu neque. Morbi pulvinar aliquam rutrum.

Donec fringilla arcu quis purus vehicula feugiat. Etiam sed erat sit amet elit blandit facilisis quis vitae tortor. Nullam et ornare enim, et hendrerit orci. Phasellus non nisi et sem rutrum vulputate non et dui. Aliquam eget scelerisque eros. Ut eleifend urna ipsum, sit amet tempor lorem fringilla eget. Vestibulum vitae urna nisl. Cras consequat posuere dui, vel suscipit ante convallis in. Phasellus libero urna, elementum viverra interdum laoreet, aliquet faucibus ex. Duis urna ipsum, ornare nec ullamcorper at, aliquet at purus. Cras vestibulum vulputate aliquet. Integer luctus arcu arcu, vel fringilla eros tincidunt et. Integer dignissim dictum fermentum. Aenean porttitor congue leo in ultricies.

Praesent felis felis, tempus eu dapibus ut, fringilla a turpis. Vivamus vel commodo nisi. Praesent vel quam a mauris porttitor placerat sed id erat. Donec lacinia feugiat dui, sit amet accumsan libero sagittis vel. Praesent lectus est, pellentesque nec nisl at, sagittis scelerisque mi. Donec sit amet tortor nisi. Nam imperdiet, metus faucibus egestas interdum, ipsum diam tempus metus, eget dapibus lacus felis commodo elit. Fusce quis augue ut sem lacinia gravida. Mauris elit enim, porta ut fringilla hendrerit, gravida nec nisl. Nullam bibendum pharetra velit vel dignissim.

Curabitur sed risus commodo, mollis felis ac, bibendum magna. Aenean porttitor bibendum purus, id bibendum sapien fermentum quis. Nulla molestie non velit sollicitudin elementum. Nam massa lectus, egestas at lectus non, accumsan mattis velit. Nulla justo eros, suscipit vitae venenatis pellentesque, sollicitudin ac mi. Integer tempus et elit quis efficitur. Praesent vitae semper tortor, sed posuere diam. In molestie leo at ex feugiat auctor. Nunc quis dui nec urna venenatis maximus. Praesent eget vestibulum elit, vel pellentesque nibh. Mauris facilisis ante vel auctor molestie. Nunc hendrerit, ligula ut sagittis bibendum, est nunc tincidunt neque, non facilisis ipsum lacus et nisl. In hac habitasse platea dictumst. Phasellus scelerisque ac quam vitae faucibus. Nunc dapibus mi vel convallis vestibulum.

Etiam facilisis a est ut fermentum. Etiam vitae lorem vel arcu accumsan tempor id eu libero. Aliquam et dignissim leo. Nulla quis faucibus ante, tempor porttitor dui. Ut mi felis, accumsan ac metus a, placerat fringilla velit. Duis eget molestie turpis, in rutrum orci. Suspendisse ornare leo et molestie posuere. Pellentesque dapibus vulputate lacus, eget ultrices eros eleifend vitae. Curabitur scelerisque metus odio, nec tincidunt neque pellentesque in. Aenean quis nisl vitae nulla congue pulvinar. Integer lacinia sem sit amet rutrum volutpat. Nullam non tellus nec nulla fringilla maximus ac hendrerit tortor. Aliquam erat volutpat. Duis felis augue, euismod vitae libero vitae, sollicitudin accumsan nisl. Donec est purus, sodales feugiat aliquet sed, scelerisque quis ligula. Pellentesque habitant morbi tristique senectus et netus et malesuada fames ac turpis egestas.

Cras accumsan pretium lacus ut dictum. Phasellus sagittis faucibus nulla iaculis maximus. Nullam vel ligula ut elit euismod imperdiet at vel lectus. Aliquam erat volutpat. Aliquam in enim in neque ultrices accumsan. Sed et consectetur libero. Donec dignissim, augue a rhoncus convallis, purus arcu commodo lacus, in dignissim sapien urna in purus. Nullam ullamcorper dictum condimentum. Integer vel ullamcorper nisl. Nam tellus est, rutrum ac mattis eu, eleifend vitae turpis.

Nullam iaculis consequat pretium. Aliquam et varius nisl. Mauris eget sem in justo molestie finibus. Cras in purus sit amet lectus rhoncus facilisis non eget odio. Vivamus sit amet odio ac turpis consequat ultrices in non justo. Phasellus porta, nunc sed vestibulum semper, dolor enim pharetra ipsum, vitae rutrum quam felis fringilla lorem. Quisque iaculis, ex in consequat facilisis, justo nulla auctor ante, sollicitudin ullamcorper nulla mauris eu tortor. Quisque eros mi, vehicula vitae arcu ut, scelerisque bibendum risus. Fusce faucibus gravida erat, non ultricies sem volutpat hendrerit. Integer eu lorem pharetra, iaculis ipsum ac, luctus leo. Vestibulum at leo justo. Ut lacus justo, vehicula consectetur massa consectetur, imperdiet dignissim sem. Sed venenatis vehicula lacus non posuere. Vestibulum tristique lacus quis imperdiet rutrum. In porttitor, velit vel vehicula scelerisque, nisi orci blandit purus, vel scelerisque orci felis nec tellus. Suspendisse euismod faucibus mi, in ornare neque.

Nulla in cursus enim, non egestas dui. Suspendisse potenti. Proin tristique porttitor scelerisque. Pellentesque condimentum pharetra augue quis blandit. Praesent ultrices leo euismod consequat blandit. Nullam ultricies lacus ut nunc dignissim, condimentum molestie tellus ultrices. Fusce id neque nec velit auctor sodales. Nullam a eros quam. Etiam eget aliquam purus. Sed condimentum purus id lorem aliquet lobortis. Nam ac purus eu ante consectetur condimentum quis a leo. Sed semper erat augue, quis dapibus arcu ultrices sit amet. Ut eget congue justo. In odio justo, fringilla eget turpis in, imperdiet facilisis risus. Donec commodo mollis sapien, eu accumsan velit efficitur at.

Maecenas vitae consequat augue. Nam laoreet leo eu metus rutrum, sit amet vestibulum tellus blandit. Donec pulvinar dui vel arcu dictum consectetur. Integer facilisis metus quis tortor eleifend, sit amet scelerisque dolor hendrerit. Praesent eleifend bibendum turpis, a congue nulla feugiat vel. Pellentesque facilisis vehicula augue. Ut quis eros at odio vehicula vulputate sit amet nec risus. Praesent rhoncus eget eros a rhoncus. Fusce condimentum lectus metus, ac blandit quam iaculis in. Donec egestas lobortis nibh quis interdum. Mauris eget ligula commodo, consectetur mauris ut, tincidunt augue. Maecenas quis vehicula quam.

Etiam nulla ex, consectetur a erat tempor, ultrices efficitur tellus. Quisque quam ligula, interdum porta vehicula eu, consectetur et libero. Nunc tincidunt elementum lacus sit amet venenatis. Aliquam venenatis maximus sollicitudin. Morbi nec dolor euismod, blandit leo id, feugiat justo. Nulla eleifend mauris ac urna fringilla, quis porttitor metus volutpat. Pellentesque consectetur ultrices eros, in ultricies velit mollis in. Proin placerat nisi sed feugiat vulputate.

Vestibulum at commodo odio, vel venenatis neque. Praesent convallis scelerisque tellus a pellentesque. Nunc tempor, est et sodales tempus, augue nibh malesuada est, sit amet accumsan leo risus et ante. Nulla odio enim, ornare et ornare at, venenatis eu felis. Vivamus accumsan pulvinar ipsum id ultricies. Fusce vel fermentum lacus. Curabitur lacinia nunc eu felis rhoncus posuere. Phasellus sollicitudin lorem ac leo finibus, nec eleifend mauris sollicitudin. Nunc porttitor, metus ut rhoncus imperdiet, sem augue eleifend lorem, a dignissim velit neque sit amet augue.

Donec odio ipsum, commodo vel maximus vitae, aliquet ut justo. Ut tellus erat, euismod id sodales vel, bibendum quis odio. Morbi pellentesque semper mauris, a cursus orci hendrerit nec. Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia curae; Mauris gravida tempus nisl ut vestibulum. Nam vulputate enim ut purus congue, vitae consequat neque placerat. Cras consectetur risus non magna molestie, et faucibus est semper. Ut auctor pulvinar nisl, et maximus erat fermentum quis. Cras posuere mattis pulvinar. Nam consectetur, turpis molestie consectetur ultricies, lectus nisi consequat est, eu imperdiet nisl orci auctor urna. Ut nisi nibh, dictum a aliquam quis, maximus sed lacus. Integer ex ipsum, tempor in pulvinar vitae, blandit a magna. Morbi tristique dictum erat in sagittis. Vestibulum enim neque, rhoncus eu ipsum ac, vestibulum auctor diam. Cras ut mollis eros.
""".ToCharArray();

    [Params(10, 100, 1000)]
    public int EditCount;

    [Benchmark]
    public void ListConstructionOverhead() => new List<char>();

    [Benchmark]
    public void RopeConstructionOverhead() => new Rope<char>(ReadOnlyMemory<char>.Empty);

    [Benchmark]
    public void ListAppend()
    {
        var s = new List<char>(LoremIpsum);
        for (int i = 0; i < EditCount; i++)
        {
            s.AddRange(LoremIpsum);
        }

        ////s.ToString();
    }

    [Benchmark]
    public void RopeAppend()
    {
        var lorem = LoremIpsum.ToRope();
        var s = lorem;
        for (int i = 0; i < EditCount; i++)
        {
            s += lorem;
        }

        ////s.ToString();
    }

    [Benchmark]
    public void ListInsert()
    {
        var s = new List<char>(LoremIpsum);
        for (int i = 0; i < EditCount; i++)
        {
            s.InsertRange(321, LoremIpsum);
        }

        ////s.ToString();
    }

    [Benchmark]
    public void RopeInsert()
    {
        var lorem = LoremIpsum.ToRope();
        var s = lorem;
        for (int i = 0; i < EditCount; i++)
        {
            s = s.Insert(321, lorem);
        }

        ////s.ToString();
    }

    [Benchmark]
    public void ListSplitConcat()
    {
        var s = new List<char>(LoremIpsum);
        for (int i = 0; i < EditCount; i++)
        {
            s.RemoveRange(321, s.Count - 322); //  =  new StringBuilder(s.ToString()[..321]);
            s.AddRange(LoremIpsum);
        }

        ////s.ToString();
    }

    [Benchmark]
    public void RopeSplitConcat()
    {
        var lorem = LoremIpsum.ToRope();
        var s = lorem;
        for (int i = 0; i < EditCount; i++)
        {
            s = s[..321];
            s = s + lorem;
        }

        ////s.ToString();
    }
}