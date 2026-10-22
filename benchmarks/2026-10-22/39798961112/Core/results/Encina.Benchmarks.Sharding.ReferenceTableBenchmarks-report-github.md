```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3


```
| Method                                     | Job        | IterationCount | LaunchCount | WarmupCount | Mean             | Error          | StdDev         | Median           | Ratio  | RatioSD | Rank | Gen0    | Allocated | Alloc Ratio |
|------------------------------------------- |----------- |--------------- |------------ |------------ |-----------------:|---------------:|---------------:|-----------------:|-------:|--------:|-----:|--------:|----------:|------------:|
| &#39;Hash 100 rows&#39;                            | DefaultJob | Default        | Default     | Default     |    74,072.121 ns |    194.8135 ns |    172.6971 ns |    74,025.109 ns |  1.000 |    0.00 |    6 |  0.8545 |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | DefaultJob | Default        | Default     | Default     |   791,630.155 ns |  1,587.2068 ns |  1,325.3895 ns |   791,254.758 ns | 10.687 |    0.03 |    7 |  7.8125 |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | DefaultJob | Default        | Default     | Default     | 4,371,724.732 ns |  6,165.5714 ns |  4,813.6726 ns | 4,372,609.004 ns | 59.020 |    0.15 |    8 | 39.0625 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | DefaultJob | Default        | Default     | Default     |        10.665 ns |      0.2402 ns |      0.2247 ns |        10.594 ns |  0.000 |    0.00 |    3 |  0.0014 |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | DefaultJob | Default        | Default     | Default     |         7.406 ns |      0.0074 ns |      0.0062 ns |         7.405 ns |  0.000 |    0.00 |    1 |       - |         - |       0.000 |
| Registry.GetConfiguration                  | DefaultJob | Default        | Default     | Default     |         9.144 ns |      0.0052 ns |      0.0043 ns |         9.143 ns |  0.000 |    0.00 |    2 |       - |         - |       0.000 |
| Registry.GetAllConfigurations              | DefaultJob | Default        | Default     | Default     |       139.919 ns |      0.3651 ns |      0.3236 ns |       139.907 ns |  0.002 |    0.00 |    5 |  0.0067 |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | DefaultJob | Default        | Default     | Default     |        15.885 ns |      0.0148 ns |      0.0132 ns |        15.881 ns |  0.000 |    0.00 |    4 |       - |         - |       0.000 |
|                                            |            |                |             |             |                  |                |                |                  |        |         |      |         |           |             |
| &#39;Hash 100 rows&#39;                            | MediumRun  | 15             | 2           | 10          |    74,907.053 ns |    244.2888 ns |    350.3517 ns |    74,992.195 ns |  1.000 |    0.01 |    6 |  0.8545 |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | MediumRun  | 15             | 2           | 10          |   803,380.446 ns |    887.8681 ns |  1,215.3240 ns |   803,547.104 ns | 10.725 |    0.05 |    7 |  7.8125 |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | MediumRun  | 15             | 2           | 10          | 4,368,479.833 ns | 10,676.1043 ns | 15,648.8966 ns | 4,366,458.969 ns | 58.320 |    0.34 |    8 | 39.0625 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | MediumRun  | 15             | 2           | 10          |         9.958 ns |      0.1241 ns |      0.1780 ns |         9.919 ns |  0.000 |    0.00 |    3 |  0.0014 |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | MediumRun  | 15             | 2           | 10          |         7.400 ns |      0.0035 ns |      0.0049 ns |         7.399 ns |  0.000 |    0.00 |    1 |       - |         - |       0.000 |
| Registry.GetConfiguration                  | MediumRun  | 15             | 2           | 10          |         9.295 ns |      0.1387 ns |      0.1989 ns |         9.223 ns |  0.000 |    0.00 |    2 |       - |         - |       0.000 |
| Registry.GetAllConfigurations              | MediumRun  | 15             | 2           | 10          |       137.360 ns |      0.9172 ns |      1.3444 ns |       138.020 ns |  0.002 |    0.00 |    5 |  0.0067 |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | MediumRun  | 15             | 2           | 10          |        16.006 ns |      0.1177 ns |      0.1726 ns |        15.895 ns |  0.000 |    0.00 |    4 |       - |         - |       0.000 |
