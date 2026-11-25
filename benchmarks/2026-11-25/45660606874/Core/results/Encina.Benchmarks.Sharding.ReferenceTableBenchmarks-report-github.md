```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                                     | Job        | IterationCount | LaunchCount | WarmupCount | Mean             | Error          | StdDev         | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------- |----------- |--------------- |------------ |------------ |-----------------:|---------------:|---------------:|-------:|--------:|-----:|-------:|----------:|------------:|
| &#39;Hash 100 rows&#39;                            | DefaultJob | Default        | Default     | Default     |    70,113.470 ns |    134.6363 ns |    119.3516 ns |  1.000 |    0.00 |    6 | 0.1221 |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | DefaultJob | Default        | Default     | Default     |   761,782.874 ns |  1,655.7334 ns |  1,467.7645 ns | 10.865 |    0.03 |    7 | 0.9766 |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | DefaultJob | Default        | Default     | Default     | 4,202,233.894 ns | 14,768.6720 ns | 13,814.6250 ns | 59.935 |    0.21 |    8 | 7.8125 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | DefaultJob | Default        | Default     | Default     |        10.769 ns |      0.0418 ns |      0.0370 ns |  0.000 |    0.00 |    3 | 0.0003 |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | DefaultJob | Default        | Default     | Default     |         8.165 ns |      0.0255 ns |      0.0226 ns |  0.000 |    0.00 |    1 |      - |         - |       0.000 |
| Registry.GetConfiguration                  | DefaultJob | Default        | Default     | Default     |         9.509 ns |      0.1076 ns |      0.1007 ns |  0.000 |    0.00 |    2 |      - |         - |       0.000 |
| Registry.GetAllConfigurations              | DefaultJob | Default        | Default     | Default     |       144.995 ns |      0.2994 ns |      0.2801 ns |  0.002 |    0.00 |    5 | 0.0012 |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | DefaultJob | Default        | Default     | Default     |        16.409 ns |      0.0959 ns |      0.0897 ns |  0.000 |    0.00 |    4 |      - |         - |       0.000 |
|                                            |            |                |             |             |                  |                |                |        |         |      |        |           |             |
| &#39;Hash 100 rows&#39;                            | ShortRun   | 3              | 1           | 3           |    70,518.034 ns |  4,509.9504 ns |    247.2056 ns |  1.000 |    0.00 |    5 | 0.1221 |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | ShortRun   | 3              | 1           | 3           |   761,889.237 ns |  4,314.7323 ns |    236.5050 ns | 10.804 |    0.03 |    6 | 0.9766 |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | ShortRun   | 3              | 1           | 3           | 4,157,256.763 ns | 71,136.1387 ns |  3,899.2118 ns | 58.954 |    0.19 |    7 | 7.8125 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | ShortRun   | 3              | 1           | 3           |        10.539 ns |      0.9993 ns |      0.0548 ns |  0.000 |    0.00 |    2 | 0.0003 |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | ShortRun   | 3              | 1           | 3           |         7.394 ns |      0.0871 ns |      0.0048 ns |  0.000 |    0.00 |    1 |      - |         - |       0.000 |
| Registry.GetConfiguration                  | ShortRun   | 3              | 1           | 3           |         9.369 ns |      5.7087 ns |      0.3129 ns |  0.000 |    0.00 |    2 |      - |         - |       0.000 |
| Registry.GetAllConfigurations              | ShortRun   | 3              | 1           | 3           |       143.879 ns |      6.6051 ns |      0.3620 ns |  0.002 |    0.00 |    4 | 0.0012 |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | ShortRun   | 3              | 1           | 3           |        18.269 ns |      0.8923 ns |      0.0489 ns |  0.000 |    0.00 |    3 |      - |         - |       0.000 |
