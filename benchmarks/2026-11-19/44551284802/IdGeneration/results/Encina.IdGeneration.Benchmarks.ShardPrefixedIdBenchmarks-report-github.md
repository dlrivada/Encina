```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.72GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                         | Job        | IterationCount | LaunchCount | Mean     | Error    | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |---------:|---------:|--------:|------:|-------:|----------:|------------:|
| Generate_TimestampRandomFormat | Job-YFEFPZ | 10             | Default     | 809.7 ns |  4.53 ns | 2.99 ns |  1.15 | 0.0057 |     151 B |        1.26 |
| Generate_ToString              | Job-YFEFPZ | 10             | Default     | 785.8 ns |  4.58 ns | 2.72 ns |  1.12 | 0.0086 |     216 B |        1.80 |
| Generate_UlidFormat            | Job-YFEFPZ | 10             | Default     | 702.1 ns |  3.56 ns | 2.36 ns |  1.00 | 0.0048 |     120 B |        1.00 |
| Generate_UuidV7Format          | Job-YFEFPZ | 10             | Default     | 672.9 ns |  0.65 ns | 0.34 ns |  0.96 | 0.0038 |      96 B |        0.80 |
| ExtractShardId_Ulid            | Job-YFEFPZ | 10             | Default     | 718.2 ns |  1.79 ns | 1.18 ns |  1.02 | 0.0048 |     120 B |        1.00 |
|                                |            |                |             |          |          |         |       |        |           |             |
| Generate_TimestampRandomFormat | ShortRun   | 3              | 1           | 790.2 ns |  8.77 ns | 0.48 ns |  1.12 | 0.0057 |     151 B |        1.26 |
| Generate_ToString              | ShortRun   | 3              | 1           | 812.5 ns | 24.04 ns | 1.32 ns |  1.15 | 0.0086 |     216 B |        1.80 |
| Generate_UlidFormat            | ShortRun   | 3              | 1           | 705.6 ns | 53.08 ns | 2.91 ns |  1.00 | 0.0048 |     120 B |        1.00 |
| Generate_UuidV7Format          | ShortRun   | 3              | 1           | 668.2 ns | 24.48 ns | 1.34 ns |  0.95 | 0.0038 |      96 B |        0.80 |
| ExtractShardId_Ulid            | ShortRun   | 3              | 1           | 718.0 ns | 34.27 ns | 1.88 ns |  1.02 | 0.0048 |     120 B |        1.00 |
