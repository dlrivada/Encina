```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                         | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| Generate_TimestampRandomFormat | Job-YFEFPZ | 10             | Default     | 1.271 μs | 0.0036 μs | 0.0024 μs |  1.14 | 0.0057 |     151 B |        1.26 |
| Generate_ToString              | Job-YFEFPZ | 10             | Default     | 1.230 μs | 0.0017 μs | 0.0010 μs |  1.10 | 0.0076 |     216 B |        1.80 |
| Generate_UlidFormat            | Job-YFEFPZ | 10             | Default     | 1.118 μs | 0.0086 μs | 0.0051 μs |  1.00 | 0.0038 |     120 B |        1.00 |
| Generate_UuidV7Format          | Job-YFEFPZ | 10             | Default     | 1.065 μs | 0.0107 μs | 0.0071 μs |  0.95 | 0.0038 |      96 B |        0.80 |
| ExtractShardId_Ulid            | Job-YFEFPZ | 10             | Default     | 1.100 μs | 0.0056 μs | 0.0029 μs |  0.98 | 0.0038 |     120 B |        1.00 |
|                                |            |                |             |          |           |           |       |        |           |             |
| Generate_TimestampRandomFormat | ShortRun   | 3              | 1           | 1.206 μs | 0.2254 μs | 0.0124 μs |  1.11 | 0.0057 |     151 B |        1.26 |
| Generate_ToString              | ShortRun   | 3              | 1           | 1.167 μs | 0.0926 μs | 0.0051 μs |  1.08 | 0.0076 |     216 B |        1.80 |
| Generate_UlidFormat            | ShortRun   | 3              | 1           | 1.082 μs | 0.0685 μs | 0.0038 μs |  1.00 | 0.0038 |     120 B |        1.00 |
| Generate_UuidV7Format          | ShortRun   | 3              | 1           | 1.155 μs | 0.0440 μs | 0.0024 μs |  1.07 | 0.0038 |      96 B |        0.80 |
| ExtractShardId_Ulid            | ShortRun   | 3              | 1           | 1.105 μs | 0.0580 μs | 0.0032 μs |  1.02 | 0.0038 |     120 B |        1.00 |
