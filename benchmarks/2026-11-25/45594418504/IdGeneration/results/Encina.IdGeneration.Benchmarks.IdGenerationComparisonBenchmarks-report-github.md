```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error    | StdDev  | Ratio | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|---------:|--------:|------:|-----:|-------:|----------:|------------:|
| Snowflake          | Job-YFEFPZ | 10             | Default     | 242.2 ns |  0.24 ns | 0.16 ns |  1.00 |    1 |      - |         - |          NA |
| Ulid               | Job-YFEFPZ | 10             | Default     | 426.4 ns |  0.91 ns | 0.60 ns |  1.76 |    3 | 0.0005 |      40 B |          NA |
| UuidV7             | Job-YFEFPZ | 10             | Default     | 438.0 ns |  0.68 ns | 0.45 ns |  1.81 |    4 |      - |         - |          NA |
| ShardPrefixed      | Job-YFEFPZ | 10             | Default     | 639.9 ns |  1.91 ns | 1.14 ns |  2.64 |    5 | 0.0019 |     216 B |          NA |
| DotNet_GuidNewGuid | Job-YFEFPZ | 10             | Default     | 328.6 ns |  0.94 ns | 0.56 ns |  1.36 |    2 |      - |         - |          NA |
|                    |            |                |             |          |          |         |       |      |        |           |             |
| Snowflake          | ShortRun   | 3              | 1           | 242.1 ns |  4.27 ns | 0.23 ns |  1.00 |    1 |      - |         - |          NA |
| Ulid               | ShortRun   | 3              | 1           | 426.0 ns |  6.37 ns | 0.35 ns |  1.76 |    3 | 0.0005 |      40 B |          NA |
| UuidV7             | ShortRun   | 3              | 1           | 435.4 ns | 16.83 ns | 0.92 ns |  1.80 |    3 |      - |         - |          NA |
| ShardPrefixed      | ShortRun   | 3              | 1           | 639.2 ns |  4.66 ns | 0.26 ns |  2.64 |    4 | 0.0019 |     216 B |          NA |
| DotNet_GuidNewGuid | ShortRun   | 3              | 1           | 329.9 ns |  7.09 ns | 0.39 ns |  1.36 |    2 |      - |         - |          NA |
