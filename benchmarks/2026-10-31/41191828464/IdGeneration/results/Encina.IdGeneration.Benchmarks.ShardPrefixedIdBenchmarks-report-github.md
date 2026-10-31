```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                         | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |---------:|----------:|--------:|------:|-------:|----------:|------------:|
| Generate_TimestampRandomFormat | Job-YFEFPZ | 10             | Default     | 842.6 ns |   1.55 ns | 0.92 ns |  0.94 | 0.0010 |     151 B |        1.26 |
| Generate_ToString              | Job-YFEFPZ | 10             | Default     | 957.6 ns |   7.57 ns | 5.01 ns |  1.06 | 0.0019 |     216 B |        1.80 |
| Generate_UlidFormat            | Job-YFEFPZ | 10             | Default     | 900.2 ns |   8.18 ns | 4.87 ns |  1.00 | 0.0010 |     120 B |        1.00 |
| Generate_UuidV7Format          | Job-YFEFPZ | 10             | Default     | 771.5 ns |   1.43 ns | 0.94 ns |  0.86 | 0.0010 |      96 B |        0.80 |
| ExtractShardId_Ulid            | Job-YFEFPZ | 10             | Default     | 906.2 ns |   5.39 ns | 3.57 ns |  1.01 | 0.0010 |     120 B |        1.00 |
|                                |            |                |             |          |           |         |       |        |           |             |
| Generate_TimestampRandomFormat | ShortRun   | 3              | 1           | 844.6 ns |  98.95 ns | 5.42 ns |  0.94 | 0.0010 |     151 B |        1.26 |
| Generate_ToString              | ShortRun   | 3              | 1           | 946.1 ns | 156.38 ns | 8.57 ns |  1.05 | 0.0019 |     216 B |        1.80 |
| Generate_UlidFormat            | ShortRun   | 3              | 1           | 899.5 ns |  81.36 ns | 4.46 ns |  1.00 | 0.0010 |     120 B |        1.00 |
| Generate_UuidV7Format          | ShortRun   | 3              | 1           | 767.5 ns |  45.84 ns | 2.51 ns |  0.85 | 0.0010 |      96 B |        0.80 |
| ExtractShardId_Ulid            | ShortRun   | 3              | 1           | 902.2 ns |  30.30 ns | 1.66 ns |  1.00 | 0.0010 |     120 B |        1.00 |
