```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                         | Job        | IterationCount | LaunchCount | Mean     | Error       | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |---------:|------------:|---------:|------:|--------:|-------:|----------:|------------:|
| Generate_TimestampRandomFormat | Job-YFEFPZ | 10             | Default     | 775.0 ns |     3.14 ns |  2.08 ns |  0.90 |    0.06 | 0.0010 |     151 B |        1.26 |
| Generate_ToString              | Job-YFEFPZ | 10             | Default     | 868.6 ns |    35.22 ns | 18.42 ns |  1.00 |    0.07 | 0.0019 |     216 B |        1.80 |
| Generate_UlidFormat            | Job-YFEFPZ | 10             | Default     | 869.8 ns |    97.36 ns | 64.40 ns |  1.00 |    0.10 | 0.0010 |     120 B |        1.00 |
| Generate_UuidV7Format          | Job-YFEFPZ | 10             | Default     | 765.8 ns |    80.45 ns | 53.21 ns |  0.88 |    0.08 | 0.0010 |      96 B |        0.80 |
| ExtractShardId_Ulid            | Job-YFEFPZ | 10             | Default     | 823.2 ns |     8.98 ns |  5.94 ns |  0.95 |    0.06 | 0.0010 |     120 B |        1.00 |
|                                |            |                |             |          |             |          |       |         |        |           |             |
| Generate_TimestampRandomFormat | ShortRun   | 3              | 1           | 870.6 ns |   901.81 ns | 49.43 ns |  0.92 |    0.05 | 0.0010 |     151 B |        1.26 |
| Generate_ToString              | ShortRun   | 3              | 1           | 875.5 ns |   264.44 ns | 14.49 ns |  0.93 |    0.02 | 0.0019 |     216 B |        1.80 |
| Generate_UlidFormat            | ShortRun   | 3              | 1           | 943.1 ns |   351.31 ns | 19.26 ns |  1.00 |    0.02 | 0.0010 |     120 B |        1.00 |
| Generate_UuidV7Format          | ShortRun   | 3              | 1           | 697.9 ns |    35.03 ns |  1.92 ns |  0.74 |    0.01 | 0.0010 |      96 B |        0.80 |
| ExtractShardId_Ulid            | ShortRun   | 3              | 1           | 891.2 ns | 1,209.85 ns | 66.32 ns |  0.95 |    0.06 | 0.0010 |     120 B |        1.00 |
