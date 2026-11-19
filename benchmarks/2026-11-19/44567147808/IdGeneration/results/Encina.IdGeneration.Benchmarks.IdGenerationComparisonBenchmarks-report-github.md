```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.26GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error    | StdDev  | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|---------:|--------:|------:|--------:|-----:|-------:|----------:|------------:|
| Snowflake          | Job-YFEFPZ | 10             | Default     | 242.2 ns |  0.14 ns | 0.09 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | Job-YFEFPZ | 10             | Default     | 422.2 ns |  0.63 ns | 0.37 ns |  1.74 |    0.00 |    3 | 0.0005 |      40 B |          NA |
| UuidV7             | Job-YFEFPZ | 10             | Default     | 431.9 ns |  0.34 ns | 0.18 ns |  1.78 |    0.00 |    3 |      - |         - |          NA |
| ShardPrefixed      | Job-YFEFPZ | 10             | Default     | 640.0 ns |  9.57 ns | 5.69 ns |  2.64 |    0.02 |    4 | 0.0019 |     216 B |          NA |
| DotNet_GuidNewGuid | Job-YFEFPZ | 10             | Default     | 326.5 ns |  0.45 ns | 0.27 ns |  1.35 |    0.00 |    2 |      - |         - |          NA |
|                    |            |                |             |          |          |         |       |         |      |        |           |             |
| Snowflake          | ShortRun   | 3              | 1           | 242.2 ns |  1.12 ns | 0.06 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | ShortRun   | 3              | 1           | 430.0 ns | 10.17 ns | 0.56 ns |  1.77 |    0.00 |    3 | 0.0005 |      40 B |          NA |
| UuidV7             | ShortRun   | 3              | 1           | 433.7 ns |  5.86 ns | 0.32 ns |  1.79 |    0.00 |    3 |      - |         - |          NA |
| ShardPrefixed      | ShortRun   | 3              | 1           | 644.2 ns | 25.67 ns | 1.41 ns |  2.66 |    0.01 |    4 | 0.0019 |     216 B |          NA |
| DotNet_GuidNewGuid | ShortRun   | 3              | 1           | 327.1 ns |  7.45 ns | 0.41 ns |  1.35 |    0.00 |    2 |      - |         - |          NA |
