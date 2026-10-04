```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                                     | Job        | IterationCount | LaunchCount | WarmupCount | Mean             | Error         | StdDev         | Median           | Ratio  | RatioSD | Rank | Gen0    | Allocated | Alloc Ratio |
|------------------------------------------- |----------- |--------------- |------------ |------------ |-----------------:|--------------:|---------------:|-----------------:|-------:|--------:|-----:|--------:|----------:|------------:|
| &#39;Hash 100 rows&#39;                            | DefaultJob | Default        | Default     | Default     |    74,812.653 ns |   173.0389 ns |    144.4953 ns |    74,781.870 ns |  1.000 |    0.00 |    6 |  0.8545 |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | DefaultJob | Default        | Default     | Default     |   771,340.706 ns |   966.1904 ns |    806.8128 ns |   771,449.017 ns | 10.310 |    0.02 |    7 |  7.8125 |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | DefaultJob | Default        | Default     | Default     | 4,259,788.425 ns | 8,371.6222 ns |  6,990.6837 ns | 4,260,986.305 ns | 56.940 |    0.14 |    8 | 39.0625 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | DefaultJob | Default        | Default     | Default     |        10.304 ns |     0.1140 ns |      0.0890 ns |        10.306 ns |  0.000 |    0.00 |    3 |  0.0014 |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | DefaultJob | Default        | Default     | Default     |         7.401 ns |     0.0119 ns |      0.0100 ns |         7.398 ns |  0.000 |    0.00 |    1 |       - |         - |       0.000 |
| Registry.GetConfiguration                  | DefaultJob | Default        | Default     | Default     |         9.090 ns |     0.1394 ns |      0.1235 ns |         9.135 ns |  0.000 |    0.00 |    2 |       - |         - |       0.000 |
| Registry.GetAllConfigurations              | DefaultJob | Default        | Default     | Default     |       134.184 ns |     1.4061 ns |      1.3153 ns |       134.598 ns |  0.002 |    0.00 |    5 |  0.0067 |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | DefaultJob | Default        | Default     | Default     |        15.879 ns |     0.0138 ns |      0.0122 ns |        15.878 ns |  0.000 |    0.00 |    4 |       - |         - |       0.000 |
|                                            |            |                |             |             |                  |               |                |                  |        |         |      |         |           |             |
| &#39;Hash 100 rows&#39;                            | MediumRun  | 15             | 2           | 10          |    73,567.821 ns |    93.0397 ns |    133.4348 ns |    73,523.175 ns |  1.000 |    0.00 |    5 |  0.8545 |   14560 B |       1.000 |
| &#39;Hash 1000 rows&#39;                           | MediumRun  | 15             | 2           | 10          |   779,243.222 ns | 1,832.4501 ns |  2,508.2786 ns |   779,065.430 ns | 10.592 |    0.04 |    6 |  7.8125 |  140560 B |       9.654 |
| &#39;Hash 5000 rows&#39;                           | MediumRun  | 15             | 2           | 10          | 4,192,699.899 ns | 9,336.5033 ns | 13,685.3265 ns | 4,197,410.430 ns | 56.991 |    0.21 |    7 | 39.0625 |  700560 B |      48.115 |
| &#39;Hash empty collection&#39;                    | MediumRun  | 15             | 2           | 10          |        10.057 ns |     0.6623 ns |      0.9913 ns |         9.835 ns |  0.000 |    0.00 |    2 |  0.0014 |      24 B |       0.002 |
| &#39;Registry.IsRegistered (hit)&#39;              | MediumRun  | 15             | 2           | 10          |         7.321 ns |     0.0651 ns |      0.0933 ns |         7.389 ns |  0.000 |    0.00 |    1 |       - |         - |       0.000 |
| Registry.GetConfiguration                  | MediumRun  | 15             | 2           | 10          |         9.259 ns |     0.1339 ns |      0.1963 ns |         9.140 ns |  0.000 |    0.00 |    2 |       - |         - |       0.000 |
| Registry.GetAllConfigurations              | MediumRun  | 15             | 2           | 10          |       136.194 ns |     1.0411 ns |      1.4931 ns |       135.534 ns |  0.002 |    0.00 |    4 |  0.0067 |     112 B |       0.008 |
| &#39;EntityMetadataCache.GetOrCreate (cached)&#39; | MediumRun  | 15             | 2           | 10          |        15.834 ns |     0.0656 ns |      0.0962 ns |        15.866 ns |  0.000 |    0.00 |    3 |       - |         - |       0.000 |
