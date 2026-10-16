```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean       | Error    | StdDev  | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |-----------:|---------:|--------:|------:|--------:|-----:|-------:|----------:|------------:|
| Snowflake          | Job-YFEFPZ | 10             | Default     |   241.6 ns |  0.12 ns | 0.08 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | Job-YFEFPZ | 10             | Default     |   975.4 ns |  9.34 ns | 5.56 ns |  4.04 |    0.02 |    3 |      - |      40 B |          NA |
| UuidV7             | Job-YFEFPZ | 10             | Default     |   974.2 ns |  1.07 ns | 0.71 ns |  4.03 |    0.00 |    3 |      - |         - |          NA |
| ShardPrefixed      | Job-YFEFPZ | 10             | Default     | 1,189.0 ns |  5.68 ns | 3.76 ns |  4.92 |    0.01 |    4 | 0.0076 |     216 B |          NA |
| DotNet_GuidNewGuid | Job-YFEFPZ | 10             | Default     |   397.1 ns |  0.40 ns | 0.24 ns |  1.64 |    0.00 |    2 |      - |         - |          NA |
|                    |            |                |             |            |          |         |       |         |      |        |           |             |
| Snowflake          | ShortRun   | 3              | 1           |   241.6 ns |  1.72 ns | 0.09 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | ShortRun   | 3              | 1           |   971.5 ns | 32.60 ns | 1.79 ns |  4.02 |    0.01 |    3 |      - |      40 B |          NA |
| UuidV7             | ShortRun   | 3              | 1           |   968.6 ns |  7.85 ns | 0.43 ns |  4.01 |    0.00 |    3 |      - |         - |          NA |
| ShardPrefixed      | ShortRun   | 3              | 1           | 1,256.0 ns | 93.16 ns | 5.11 ns |  5.20 |    0.02 |    4 | 0.0076 |     216 B |          NA |
| DotNet_GuidNewGuid | ShortRun   | 3              | 1           |   398.4 ns | 29.30 ns | 1.61 ns |  1.65 |    0.01 |    2 |      - |         - |          NA |
