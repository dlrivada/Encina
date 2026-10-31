```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| Snowflake          | Job-YFEFPZ | 10             | Default     |   241.6 ns |   0.11 ns |  0.07 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | Job-YFEFPZ | 10             | Default     |   973.5 ns |   4.80 ns |  2.86 ns |  4.03 |    0.01 |    3 |      - |      40 B |          NA |
| UuidV7             | Job-YFEFPZ | 10             | Default     |   999.8 ns |  16.46 ns | 10.89 ns |  4.14 |    0.04 |    3 |      - |         - |          NA |
| ShardPrefixed      | Job-YFEFPZ | 10             | Default     | 1,255.5 ns |   3.47 ns |  2.06 ns |  5.20 |    0.01 |    4 | 0.0076 |     216 B |          NA |
| DotNet_GuidNewGuid | Job-YFEFPZ | 10             | Default     |   405.3 ns |   0.42 ns |  0.28 ns |  1.68 |    0.00 |    2 |      - |         - |          NA |
|                    |            |                |             |            |           |          |       |         |      |        |           |             |
| Snowflake          | ShortRun   | 3              | 1           |   241.6 ns |   1.93 ns |  0.11 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | ShortRun   | 3              | 1           |   974.3 ns |  63.19 ns |  3.46 ns |  4.03 |    0.01 |    3 |      - |      40 B |          NA |
| UuidV7             | ShortRun   | 3              | 1           |   977.0 ns |   7.55 ns |  0.41 ns |  4.04 |    0.00 |    3 |      - |         - |          NA |
| ShardPrefixed      | ShortRun   | 3              | 1           | 1,229.6 ns | 197.14 ns | 10.81 ns |  5.09 |    0.04 |    4 | 0.0076 |     216 B |          NA |
| DotNet_GuidNewGuid | ShortRun   | 3              | 1           |   405.3 ns |  15.94 ns |  0.87 ns |  1.68 |    0.00 |    2 |      - |         - |          NA |
