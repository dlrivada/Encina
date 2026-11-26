```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                         | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |---------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Generate_TimestampRandomFormat | Job-YFEFPZ | 10             | Default     | 542.8 ns |   1.79 ns |  1.19 ns |  0.92 |    0.01 | 0.0010 |     151 B |        1.26 |
| Generate_ToString              | Job-YFEFPZ | 10             | Default     | 630.5 ns |   3.44 ns |  2.05 ns |  1.07 |    0.01 | 0.0019 |     216 B |        1.80 |
| Generate_UlidFormat            | Job-YFEFPZ | 10             | Default     | 588.6 ns |   7.46 ns |  4.44 ns |  1.00 |    0.01 | 0.0010 |     120 B |        1.00 |
| Generate_UuidV7Format          | Job-YFEFPZ | 10             | Default     | 471.6 ns |   1.00 ns |  0.52 ns |  0.80 |    0.01 | 0.0010 |      96 B |        0.80 |
| ExtractShardId_Ulid            | Job-YFEFPZ | 10             | Default     | 592.9 ns |   5.39 ns |  3.56 ns |  1.01 |    0.01 | 0.0010 |     120 B |        1.00 |
|                                |            |                |             |          |           |          |       |         |        |           |             |
| Generate_TimestampRandomFormat | ShortRun   | 3              | 1           | 550.7 ns |   6.56 ns |  0.36 ns |  0.93 |    0.00 | 0.0010 |     151 B |        1.26 |
| Generate_ToString              | ShortRun   | 3              | 1           | 634.8 ns |  27.72 ns |  1.52 ns |  1.07 |    0.01 | 0.0019 |     216 B |        1.80 |
| Generate_UlidFormat            | ShortRun   | 3              | 1           | 594.2 ns |  54.08 ns |  2.96 ns |  1.00 |    0.01 | 0.0010 |     120 B |        1.00 |
| Generate_UuidV7Format          | ShortRun   | 3              | 1           | 471.6 ns |  20.10 ns |  1.10 ns |  0.79 |    0.00 | 0.0010 |      96 B |        0.80 |
| ExtractShardId_Ulid            | ShortRun   | 3              | 1           | 630.7 ns | 357.77 ns | 19.61 ns |  1.06 |    0.03 | 0.0010 |     120 B |        1.00 |
