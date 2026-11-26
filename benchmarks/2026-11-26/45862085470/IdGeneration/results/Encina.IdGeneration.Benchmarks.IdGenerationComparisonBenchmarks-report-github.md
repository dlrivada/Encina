```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev  | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|--------:|------:|--------:|-----:|-------:|----------:|------------:|
| Snowflake          | Job-YFEFPZ | 10             | Default     | 242.3 ns |   0.06 ns | 0.04 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | Job-YFEFPZ | 10             | Default     | 701.4 ns |   9.45 ns | 6.25 ns |  2.89 |    0.02 |    2 | 0.0019 |      40 B |          NA |
| UuidV7             | Job-YFEFPZ | 10             | Default     | 679.5 ns |   1.09 ns | 0.65 ns |  2.80 |    0.00 |    2 |      - |         - |          NA |
| ShardPrefixed      | Job-YFEFPZ | 10             | Default     | 852.3 ns |   7.99 ns | 4.75 ns |  3.52 |    0.02 |    3 | 0.0124 |     216 B |          NA |
| DotNet_GuidNewGuid | Job-YFEFPZ | 10             | Default     | 681.5 ns |   1.86 ns | 1.23 ns |  2.81 |    0.00 |    2 |      - |         - |          NA |
|                    |            |                |             |          |           |         |       |         |      |        |           |             |
| Snowflake          | ShortRun   | 3              | 1           | 242.3 ns |   0.73 ns | 0.04 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | ShortRun   | 3              | 1           | 658.8 ns |  27.23 ns | 1.49 ns |  2.72 |    0.01 |    2 | 0.0019 |      40 B |          NA |
| UuidV7             | ShortRun   | 3              | 1           | 655.3 ns |  79.72 ns | 4.37 ns |  2.70 |    0.02 |    2 |      - |         - |          NA |
| ShardPrefixed      | ShortRun   | 3              | 1           | 859.9 ns | 107.73 ns | 5.91 ns |  3.55 |    0.02 |    3 | 0.0124 |     216 B |          NA |
| DotNet_GuidNewGuid | ShortRun   | 3              | 1           | 677.9 ns |   2.71 ns | 0.15 ns |  2.80 |    0.00 |    2 |      - |         - |          NA |
