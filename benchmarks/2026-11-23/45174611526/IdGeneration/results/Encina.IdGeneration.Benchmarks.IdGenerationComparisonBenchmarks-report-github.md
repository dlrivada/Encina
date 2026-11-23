```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.02GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev  | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|--------:|------:|--------:|-----:|-------:|----------:|------------:|
| Snowflake          | Job-YFEFPZ | 10             | Default     | 242.2 ns |   0.12 ns | 0.08 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | Job-YFEFPZ | 10             | Default     | 426.3 ns |   2.76 ns | 1.83 ns |  1.76 |    0.01 |    3 | 0.0005 |      40 B |          NA |
| UuidV7             | Job-YFEFPZ | 10             | Default     | 435.9 ns |   0.66 ns | 0.44 ns |  1.80 |    0.00 |    3 |      - |         - |          NA |
| ShardPrefixed      | Job-YFEFPZ | 10             | Default     | 646.9 ns |   1.90 ns | 1.13 ns |  2.67 |    0.00 |    4 | 0.0019 |     216 B |          NA |
| DotNet_GuidNewGuid | Job-YFEFPZ | 10             | Default     | 328.0 ns |   0.72 ns | 0.43 ns |  1.35 |    0.00 |    2 |      - |         - |          NA |
|                    |            |                |             |          |           |         |       |         |      |        |           |             |
| Snowflake          | ShortRun   | 3              | 1           | 242.1 ns |   1.90 ns | 0.10 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | ShortRun   | 3              | 1           | 426.1 ns |   6.12 ns | 0.34 ns |  1.76 |    0.00 |    3 | 0.0005 |      40 B |          NA |
| UuidV7             | ShortRun   | 3              | 1           | 432.6 ns |  16.70 ns | 0.92 ns |  1.79 |    0.00 |    3 |      - |         - |          NA |
| ShardPrefixed      | ShortRun   | 3              | 1           | 648.6 ns | 164.18 ns | 9.00 ns |  2.68 |    0.03 |    4 | 0.0019 |     216 B |          NA |
| DotNet_GuidNewGuid | ShortRun   | 3              | 1           | 326.8 ns |   5.36 ns | 0.29 ns |  1.35 |    0.00 |    2 |      - |         - |          NA |
