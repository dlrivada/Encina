```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev  | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |-----------:|----------:|--------:|------:|--------:|-----:|-------:|----------:|------------:|
| Snowflake          | Job-YFEFPZ | 10             | Default     |   241.3 ns |   0.08 ns | 0.05 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | Job-YFEFPZ | 10             | Default     |   979.1 ns |   7.13 ns | 4.72 ns |  4.06 |    0.02 |    3 |      - |      40 B |          NA |
| UuidV7             | Job-YFEFPZ | 10             | Default     |   988.3 ns |   1.92 ns | 1.27 ns |  4.10 |    0.01 |    3 |      - |         - |          NA |
| ShardPrefixed      | Job-YFEFPZ | 10             | Default     | 1,286.4 ns |  15.11 ns | 9.99 ns |  5.33 |    0.04 |    4 | 0.0019 |     216 B |          NA |
| DotNet_GuidNewGuid | Job-YFEFPZ | 10             | Default     |   451.4 ns |   3.97 ns | 2.63 ns |  1.87 |    0.01 |    2 |      - |         - |          NA |
|                    |            |                |             |            |           |         |       |         |      |        |           |             |
| Snowflake          | ShortRun   | 3              | 1           |   241.2 ns |   0.62 ns | 0.03 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | ShortRun   | 3              | 1           |   991.6 ns | 141.08 ns | 7.73 ns |  4.11 |    0.03 |    3 |      - |      40 B |          NA |
| UuidV7             | ShortRun   | 3              | 1           |   986.7 ns |  22.23 ns | 1.22 ns |  4.09 |    0.00 |    3 |      - |         - |          NA |
| ShardPrefixed      | ShortRun   | 3              | 1           | 1,278.3 ns | 133.98 ns | 7.34 ns |  5.30 |    0.03 |    4 | 0.0019 |     216 B |          NA |
| DotNet_GuidNewGuid | ShortRun   | 3              | 1           |   449.8 ns |  46.17 ns | 2.53 ns |  1.86 |    0.01 |    2 |      - |         - |          NA |
