```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                         | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|-------:|----------:|------------:|
| Generate_TimestampRandomFormat | Job-YFEFPZ | 10             | Default     | 1,020.3 ns |   3.09 ns |  1.84 ns |  1.13 | 0.0076 |     151 B |        1.26 |
| Generate_ToString              | Job-YFEFPZ | 10             | Default     | 1,004.4 ns |   6.07 ns |  3.61 ns |  1.11 | 0.0114 |     216 B |        1.80 |
| Generate_UlidFormat            | Job-YFEFPZ | 10             | Default     |   902.1 ns |   5.77 ns |  3.43 ns |  1.00 | 0.0067 |     120 B |        1.00 |
| Generate_UuidV7Format          | Job-YFEFPZ | 10             | Default     |   853.7 ns |   3.49 ns |  2.31 ns |  0.95 | 0.0057 |      96 B |        0.80 |
| ExtractShardId_Ulid            | Job-YFEFPZ | 10             | Default     |   904.4 ns |   4.58 ns |  3.03 ns |  1.00 | 0.0067 |     120 B |        1.00 |
|                                |            |                |             |            |           |          |       |        |           |             |
| Generate_TimestampRandomFormat | ShortRun   | 3              | 1           | 1,020.5 ns | 204.54 ns | 11.21 ns |  1.15 | 0.0076 |     151 B |        1.26 |
| Generate_ToString              | ShortRun   | 3              | 1           |   975.9 ns |  38.39 ns |  2.10 ns |  1.10 | 0.0114 |     216 B |        1.80 |
| Generate_UlidFormat            | ShortRun   | 3              | 1           |   887.1 ns |  34.40 ns |  1.89 ns |  1.00 | 0.0067 |     120 B |        1.00 |
| Generate_UuidV7Format          | ShortRun   | 3              | 1           |   892.3 ns |  22.20 ns |  1.22 ns |  1.01 | 0.0057 |      96 B |        0.80 |
| ExtractShardId_Ulid            | ShortRun   | 3              | 1           |   927.2 ns | 141.71 ns |  7.77 ns |  1.05 | 0.0067 |     120 B |        1.00 |
