```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| Snowflake          | Job-YFEFPZ | 10             | Default     | 242.3 ns |   0.10 ns |  0.06 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | Job-YFEFPZ | 10             | Default     | 673.7 ns |   6.56 ns |  3.91 ns |  2.78 |    0.02 |    2 | 0.0019 |      40 B |          NA |
| UuidV7             | Job-YFEFPZ | 10             | Default     | 658.6 ns |   4.44 ns |  2.64 ns |  2.72 |    0.01 |    2 |      - |         - |          NA |
| ShardPrefixed      | Job-YFEFPZ | 10             | Default     | 864.1 ns |  22.76 ns | 13.55 ns |  3.57 |    0.05 |    3 | 0.0124 |     216 B |          NA |
| DotNet_GuidNewGuid | Job-YFEFPZ | 10             | Default     | 686.5 ns |   2.01 ns |  1.20 ns |  2.83 |    0.00 |    2 |      - |         - |          NA |
|                    |            |                |             |          |           |          |       |         |      |        |           |             |
| Snowflake          | ShortRun   | 3              | 1           | 242.3 ns |   0.87 ns |  0.05 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | ShortRun   | 3              | 1           | 736.1 ns | 142.54 ns |  7.81 ns |  3.04 |    0.03 |    2 | 0.0019 |      40 B |          NA |
| UuidV7             | ShortRun   | 3              | 1           | 653.9 ns |  44.05 ns |  2.41 ns |  2.70 |    0.01 |    2 |      - |         - |          NA |
| ShardPrefixed      | ShortRun   | 3              | 1           | 868.1 ns | 249.14 ns | 13.66 ns |  3.58 |    0.05 |    2 | 0.0124 |     216 B |          NA |
| DotNet_GuidNewGuid | ShortRun   | 3              | 1           | 685.9 ns |   5.38 ns |  0.30 ns |  2.83 |    0.00 |    2 |      - |         - |          NA |
