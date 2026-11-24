```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.72GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                         | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |---------:|----------:|--------:|------:|-------:|----------:|------------:|
| Generate_TimestampRandomFormat | Job-YFEFPZ | 10             | Default     | 548.8 ns |   6.35 ns | 4.20 ns |  0.91 | 0.0010 |     151 B |        1.26 |
| Generate_ToString              | Job-YFEFPZ | 10             | Default     | 642.2 ns |   4.44 ns | 2.64 ns |  1.07 | 0.0019 |     216 B |        1.80 |
| Generate_UlidFormat            | Job-YFEFPZ | 10             | Default     | 600.7 ns |   5.65 ns | 3.74 ns |  1.00 | 0.0010 |     120 B |        1.00 |
| Generate_UuidV7Format          | Job-YFEFPZ | 10             | Default     | 478.0 ns |   1.17 ns | 0.70 ns |  0.80 | 0.0010 |      96 B |        0.80 |
| ExtractShardId_Ulid            | Job-YFEFPZ | 10             | Default     | 601.7 ns |   3.03 ns | 2.01 ns |  1.00 | 0.0010 |     120 B |        1.00 |
|                                |            |                |             |          |           |         |       |        |           |             |
| Generate_TimestampRandomFormat | ShortRun   | 3              | 1           | 542.8 ns |  47.66 ns | 2.61 ns |  0.91 | 0.0010 |     151 B |        1.26 |
| Generate_ToString              | ShortRun   | 3              | 1           | 638.0 ns |  19.72 ns | 1.08 ns |  1.07 | 0.0019 |     216 B |        1.80 |
| Generate_UlidFormat            | ShortRun   | 3              | 1           | 597.7 ns | 100.34 ns | 5.50 ns |  1.00 | 0.0010 |     120 B |        1.00 |
| Generate_UuidV7Format          | ShortRun   | 3              | 1           | 475.7 ns |  24.86 ns | 1.36 ns |  0.80 | 0.0010 |      96 B |        0.80 |
| ExtractShardId_Ulid            | ShortRun   | 3              | 1           | 608.4 ns |  11.67 ns | 0.64 ns |  1.02 | 0.0010 |     120 B |        1.00 |
