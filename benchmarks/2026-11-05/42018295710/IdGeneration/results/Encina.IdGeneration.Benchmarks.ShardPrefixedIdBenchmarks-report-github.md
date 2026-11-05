```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 3.41GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                         | Job        | IterationCount | LaunchCount | Mean     | Error    | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |---------:|---------:|--------:|------:|-------:|----------:|------------:|
| Generate_TimestampRandomFormat | Job-YFEFPZ | 10             | Default     | 792.8 ns |  5.92 ns | 3.92 ns |  1.12 | 0.0057 |     151 B |        1.26 |
| Generate_ToString              | Job-YFEFPZ | 10             | Default     | 813.5 ns |  3.76 ns | 1.97 ns |  1.15 | 0.0086 |     216 B |        1.80 |
| Generate_UlidFormat            | Job-YFEFPZ | 10             | Default     | 705.9 ns |  2.32 ns | 1.54 ns |  1.00 | 0.0048 |     120 B |        1.00 |
| Generate_UuidV7Format          | Job-YFEFPZ | 10             | Default     | 667.0 ns |  2.02 ns | 1.20 ns |  0.94 | 0.0038 |      96 B |        0.80 |
| ExtractShardId_Ulid            | Job-YFEFPZ | 10             | Default     | 710.6 ns |  1.98 ns | 1.18 ns |  1.01 | 0.0048 |     120 B |        1.00 |
|                                |            |                |             |          |          |         |       |        |           |             |
| Generate_TimestampRandomFormat | ShortRun   | 3              | 1           | 833.2 ns | 82.87 ns | 4.54 ns |  1.19 | 0.0057 |     151 B |        1.26 |
| Generate_ToString              | ShortRun   | 3              | 1           | 775.2 ns | 20.44 ns | 1.12 ns |  1.10 | 0.0086 |     216 B |        1.80 |
| Generate_UlidFormat            | ShortRun   | 3              | 1           | 702.5 ns | 42.29 ns | 2.32 ns |  1.00 | 0.0048 |     120 B |        1.00 |
| Generate_UuidV7Format          | ShortRun   | 3              | 1           | 666.9 ns | 19.55 ns | 1.07 ns |  0.95 | 0.0038 |      96 B |        0.80 |
| ExtractShardId_Ulid            | ShortRun   | 3              | 1           | 706.0 ns | 20.06 ns | 1.10 ns |  1.00 | 0.0048 |     120 B |        1.00 |
