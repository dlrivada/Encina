```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| Snowflake          | Job-YFEFPZ | 10             | Default     | 242.3 ns |   0.13 ns |  0.09 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | Job-YFEFPZ | 10             | Default     | 656.3 ns |   3.45 ns |  2.05 ns |  2.71 |    0.01 |    3 |      - |      40 B |          NA |
| UuidV7             | Job-YFEFPZ | 10             | Default     | 676.7 ns |  35.32 ns | 21.02 ns |  2.79 |    0.08 |    3 |      - |         - |          NA |
| ShardPrefixed      | Job-YFEFPZ | 10             | Default     | 877.6 ns |  18.42 ns | 10.96 ns |  3.62 |    0.04 |    4 | 0.0019 |     216 B |          NA |
| DotNet_GuidNewGuid | Job-YFEFPZ | 10             | Default     | 325.2 ns |   0.46 ns |  0.24 ns |  1.34 |    0.00 |    2 |      - |         - |          NA |
|                    |            |                |             |          |           |          |       |         |      |        |           |             |
| Snowflake          | ShortRun   | 3              | 1           | 242.2 ns |   2.79 ns |  0.15 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | ShortRun   | 3              | 1           | 663.0 ns | 351.36 ns | 19.26 ns |  2.74 |    0.07 |    3 |      - |      40 B |          NA |
| UuidV7             | ShortRun   | 3              | 1           | 668.9 ns | 173.15 ns |  9.49 ns |  2.76 |    0.03 |    3 |      - |         - |          NA |
| ShardPrefixed      | ShortRun   | 3              | 1           | 864.6 ns |  61.68 ns |  3.38 ns |  3.57 |    0.01 |    4 | 0.0019 |     216 B |          NA |
| DotNet_GuidNewGuid | ShortRun   | 3              | 1           | 336.8 ns | 360.07 ns | 19.74 ns |  1.39 |    0.07 |    2 |      - |         - |          NA |
