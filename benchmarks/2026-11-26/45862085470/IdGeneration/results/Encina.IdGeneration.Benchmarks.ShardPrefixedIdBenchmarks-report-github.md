```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.58GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                         | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |---------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Generate_TimestampRandomFormat | Job-YFEFPZ | 10             | Default     | 655.5 ns |   4.22 ns |  2.21 ns |  0.91 |    0.01 | 0.0010 |     151 B |        1.26 |
| Generate_ToString              | Job-YFEFPZ | 10             | Default     | 776.5 ns |  18.94 ns | 12.53 ns |  1.08 |    0.02 | 0.0019 |     216 B |        1.80 |
| Generate_UlidFormat            | Job-YFEFPZ | 10             | Default     | 716.7 ns |  10.77 ns |  6.41 ns |  1.00 |    0.01 | 0.0010 |     120 B |        1.00 |
| Generate_UuidV7Format          | Job-YFEFPZ | 10             | Default     | 566.9 ns |   1.00 ns |  0.52 ns |  0.79 |    0.01 | 0.0010 |      96 B |        0.80 |
| ExtractShardId_Ulid            | Job-YFEFPZ | 10             | Default     | 714.2 ns |  15.79 ns | 10.45 ns |  1.00 |    0.02 | 0.0010 |     120 B |        1.00 |
|                                |            |                |             |          |           |          |       |         |        |           |             |
| Generate_TimestampRandomFormat | ShortRun   | 3              | 1           | 656.9 ns |  22.31 ns |  1.22 ns |  0.93 |    0.00 | 0.0010 |     151 B |        1.26 |
| Generate_ToString              | ShortRun   | 3              | 1           | 779.1 ns | 335.27 ns | 18.38 ns |  1.11 |    0.02 | 0.0019 |     216 B |        1.80 |
| Generate_UlidFormat            | ShortRun   | 3              | 1           | 704.8 ns |  13.02 ns |  0.71 ns |  1.00 |    0.00 | 0.0010 |     120 B |        1.00 |
| Generate_UuidV7Format          | ShortRun   | 3              | 1           | 609.8 ns | 260.71 ns | 14.29 ns |  0.87 |    0.02 | 0.0010 |      96 B |        0.80 |
| ExtractShardId_Ulid            | ShortRun   | 3              | 1           | 712.0 ns | 121.81 ns |  6.68 ns |  1.01 |    0.01 | 0.0010 |     120 B |        1.00 |
