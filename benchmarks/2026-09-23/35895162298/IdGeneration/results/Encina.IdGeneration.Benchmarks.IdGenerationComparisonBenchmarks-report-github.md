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
| Snowflake          | Job-YFEFPZ | 10             | Default     |   241.6 ns |  0.13 ns | 0.09 ns |  1.00 |    1 |      - |         - |          NA |
| Ulid               | Job-YFEFPZ | 10             | Default     |   957.0 ns |  3.20 ns | 1.90 ns |  3.96 |    3 |      - |      40 B |          NA |
| UuidV7             | Job-YFEFPZ | 10             | Default     |   966.6 ns |  1.36 ns | 0.81 ns |  4.00 |    3 |      - |         - |          NA |
| ShardPrefixed      | Job-YFEFPZ | 10             | Default     | 1,187.8 ns |  5.87 ns | 3.49 ns |  4.92 |    4 | 0.0076 |     216 B |          NA |
| DotNet_GuidNewGuid | Job-YFEFPZ | 10             | Default     |   396.6 ns |  1.03 ns | 0.61 ns |  1.64 |    2 |      - |         - |          NA |
|                    |            |                |             |            |          |         |       |      |        |           |             |
| Snowflake          | ShortRun   | 3              | 1           |   241.6 ns |  1.22 ns | 0.07 ns |  1.00 |    1 |      - |         - |          NA |
| Ulid               | ShortRun   | 3              | 1           |   960.2 ns | 31.92 ns | 1.75 ns |  3.97 |    3 |      - |      40 B |          NA |
| UuidV7             | ShortRun   | 3              | 1           |   960.2 ns | 40.55 ns | 2.22 ns |  3.97 |    3 |      - |         - |          NA |
| ShardPrefixed      | ShortRun   | 3              | 1           | 1,127.0 ns | 45.16 ns | 2.48 ns |  4.66 |    3 | 0.0076 |     216 B |          NA |
| DotNet_GuidNewGuid | ShortRun   | 3              | 1           |   395.9 ns |  5.59 ns | 0.31 ns |  1.64 |    2 |      - |         - |          NA |
