```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean       | Error    | StdDev  | Ratio | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |-----------:|---------:|--------:|------:|-----:|-------:|----------:|------------:|
| Snowflake          | Job-YFEFPZ | 10             | Default     |   241.7 ns |  0.08 ns | 0.05 ns |  1.00 |    1 |      - |         - |          NA |
| Ulid               | Job-YFEFPZ | 10             | Default     |   960.6 ns |  2.80 ns | 1.85 ns |  3.97 |    3 |      - |      40 B |          NA |
| UuidV7             | Job-YFEFPZ | 10             | Default     |   966.2 ns |  3.15 ns | 2.08 ns |  4.00 |    3 |      - |         - |          NA |
| ShardPrefixed      | Job-YFEFPZ | 10             | Default     | 1,163.3 ns |  4.32 ns | 2.86 ns |  4.81 |    4 | 0.0076 |     216 B |          NA |
| DotNet_GuidNewGuid | Job-YFEFPZ | 10             | Default     |   398.3 ns |  0.26 ns | 0.17 ns |  1.65 |    2 |      - |         - |          NA |
|                    |            |                |             |            |          |         |       |      |        |           |             |
| Snowflake          | ShortRun   | 3              | 1           |   241.6 ns |  1.12 ns | 0.06 ns |  1.00 |    1 |      - |         - |          NA |
| Ulid               | ShortRun   | 3              | 1           |   967.7 ns | 33.27 ns | 1.82 ns |  4.00 |    3 |      - |      40 B |          NA |
| UuidV7             | ShortRun   | 3              | 1           |   960.0 ns | 14.67 ns | 0.80 ns |  3.97 |    3 |      - |         - |          NA |
| ShardPrefixed      | ShortRun   | 3              | 1           | 1,155.1 ns | 35.52 ns | 1.95 ns |  4.78 |    3 | 0.0076 |     216 B |          NA |
| DotNet_GuidNewGuid | ShortRun   | 3              | 1           |   401.1 ns | 37.86 ns | 2.08 ns |  1.66 |    2 |      - |         - |          NA |
