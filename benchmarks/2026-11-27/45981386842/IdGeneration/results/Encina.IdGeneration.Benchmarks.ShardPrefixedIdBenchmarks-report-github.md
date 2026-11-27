```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.26GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                         | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |---------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Generate_TimestampRandomFormat | Job-YFEFPZ | 10             | Default     | 723.2 ns |  31.14 ns | 16.28 ns |  1.12 |    0.04 | 0.0086 |     151 B |        1.26 |
| Generate_ToString              | Job-YFEFPZ | 10             | Default     | 691.7 ns |  10.31 ns |  5.39 ns |  1.07 |    0.03 | 0.0124 |     216 B |        1.80 |
| Generate_UlidFormat            | Job-YFEFPZ | 10             | Default     | 644.0 ns |  24.53 ns | 16.23 ns |  1.00 |    0.03 | 0.0067 |     120 B |        1.00 |
| Generate_UuidV7Format          | Job-YFEFPZ | 10             | Default     | 584.0 ns |  11.42 ns |  7.55 ns |  0.91 |    0.02 | 0.0057 |      96 B |        0.80 |
| ExtractShardId_Ulid            | Job-YFEFPZ | 10             | Default     | 654.2 ns |   8.30 ns |  4.94 ns |  1.02 |    0.03 | 0.0067 |     120 B |        1.00 |
|                                |            |                |             |          |           |          |       |         |        |           |             |
| Generate_TimestampRandomFormat | ShortRun   | 3              | 1           | 708.8 ns |  13.75 ns |  0.75 ns |  1.13 |    0.02 | 0.0086 |     151 B |        1.26 |
| Generate_ToString              | ShortRun   | 3              | 1           | 711.5 ns | 364.96 ns | 20.00 ns |  1.13 |    0.03 | 0.0124 |     216 B |        1.80 |
| Generate_UlidFormat            | ShortRun   | 3              | 1           | 628.3 ns | 214.30 ns | 11.75 ns |  1.00 |    0.02 | 0.0067 |     120 B |        1.00 |
| Generate_UuidV7Format          | ShortRun   | 3              | 1           | 629.8 ns | 124.65 ns |  6.83 ns |  1.00 |    0.02 | 0.0057 |      96 B |        0.80 |
| ExtractShardId_Ulid            | ShortRun   | 3              | 1           | 674.6 ns |  18.69 ns |  1.02 ns |  1.07 |    0.02 | 0.0067 |     120 B |        1.00 |
