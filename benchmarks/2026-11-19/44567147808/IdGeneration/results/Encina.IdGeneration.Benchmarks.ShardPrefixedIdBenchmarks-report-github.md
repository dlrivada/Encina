```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                         | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |---------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Generate_TimestampRandomFormat | Job-YFEFPZ | 10             | Default     | 896.1 ns |   6.76 ns |  3.54 ns |  1.15 |    0.02 | 0.0086 |     151 B |        1.26 |
| Generate_ToString              | Job-YFEFPZ | 10             | Default     | 865.8 ns |  11.76 ns |  7.78 ns |  1.12 |    0.02 | 0.0124 |     216 B |        1.80 |
| Generate_UlidFormat            | Job-YFEFPZ | 10             | Default     | 776.3 ns |  21.70 ns | 14.35 ns |  1.00 |    0.02 | 0.0067 |     120 B |        1.00 |
| Generate_UuidV7Format          | Job-YFEFPZ | 10             | Default     | 745.9 ns |  11.17 ns |  7.39 ns |  0.96 |    0.02 | 0.0057 |      96 B |        0.80 |
| ExtractShardId_Ulid            | Job-YFEFPZ | 10             | Default     | 792.9 ns |  14.70 ns |  8.75 ns |  1.02 |    0.02 | 0.0067 |     120 B |        1.00 |
|                                |            |                |             |          |           |          |       |         |        |           |             |
| Generate_TimestampRandomFormat | ShortRun   | 3              | 1           | 894.7 ns |  28.83 ns |  1.58 ns |  1.10 |    0.00 | 0.0086 |     151 B |        1.26 |
| Generate_ToString              | ShortRun   | 3              | 1           | 891.9 ns | 174.12 ns |  9.54 ns |  1.09 |    0.01 | 0.0124 |     216 B |        1.80 |
| Generate_UlidFormat            | ShortRun   | 3              | 1           | 816.3 ns |  26.57 ns |  1.46 ns |  1.00 |    0.00 | 0.0067 |     120 B |        1.00 |
| Generate_UuidV7Format          | ShortRun   | 3              | 1           | 783.8 ns | 119.58 ns |  6.55 ns |  0.96 |    0.01 | 0.0057 |      96 B |        0.80 |
| ExtractShardId_Ulid            | ShortRun   | 3              | 1           | 797.2 ns |  73.31 ns |  4.02 ns |  0.98 |    0.00 | 0.0067 |     120 B |        1.00 |
