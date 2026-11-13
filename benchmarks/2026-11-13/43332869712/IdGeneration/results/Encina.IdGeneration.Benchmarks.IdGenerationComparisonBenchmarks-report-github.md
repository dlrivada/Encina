```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| Snowflake          | Job-YFEFPZ | 10             | Default     | 242.3 ns |   0.07 ns |  0.04 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | Job-YFEFPZ | 10             | Default     | 688.5 ns |   9.54 ns |  6.31 ns |  2.84 |    0.02 |    2 | 0.0019 |      40 B |          NA |
| UuidV7             | Job-YFEFPZ | 10             | Default     | 679.8 ns |   4.13 ns |  2.73 ns |  2.81 |    0.01 |    2 |      - |         - |          NA |
| ShardPrefixed      | Job-YFEFPZ | 10             | Default     | 885.1 ns |   6.98 ns |  4.15 ns |  3.65 |    0.02 |    3 | 0.0124 |     216 B |          NA |
| DotNet_GuidNewGuid | Job-YFEFPZ | 10             | Default     | 689.3 ns |   3.92 ns |  2.59 ns |  2.84 |    0.01 |    2 |      - |         - |          NA |
|                    |            |                |             |          |           |          |       |         |      |        |           |             |
| Snowflake          | ShortRun   | 3              | 1           | 242.3 ns |   0.83 ns |  0.05 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | ShortRun   | 3              | 1           | 690.9 ns | 161.31 ns |  8.84 ns |  2.85 |    0.03 |    2 | 0.0019 |      40 B |          NA |
| UuidV7             | ShortRun   | 3              | 1           | 656.3 ns |  25.95 ns |  1.42 ns |  2.71 |    0.01 |    2 |      - |         - |          NA |
| ShardPrefixed      | ShortRun   | 3              | 1           | 851.8 ns | 299.03 ns | 16.39 ns |  3.52 |    0.06 |    3 | 0.0124 |     216 B |          NA |
| DotNet_GuidNewGuid | ShortRun   | 3              | 1           | 688.0 ns |  21.67 ns |  1.19 ns |  2.84 |    0.00 |    2 |      - |         - |          NA |
