```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                         | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| Generate_TimestampRandomFormat | Job-YFEFPZ | 10             | Default     | 1.242 μs | 0.0061 μs | 0.0036 μs |  1.10 | 0.0057 |     151 B |        1.26 |
| Generate_ToString              | Job-YFEFPZ | 10             | Default     | 1.229 μs | 0.0037 μs | 0.0024 μs |  1.09 | 0.0076 |     216 B |        1.80 |
| Generate_UlidFormat            | Job-YFEFPZ | 10             | Default     | 1.127 μs | 0.0056 μs | 0.0029 μs |  1.00 | 0.0038 |     120 B |        1.00 |
| Generate_UuidV7Format          | Job-YFEFPZ | 10             | Default     | 1.078 μs | 0.0129 μs | 0.0077 μs |  0.96 | 0.0038 |      96 B |        0.80 |
| ExtractShardId_Ulid            | Job-YFEFPZ | 10             | Default     | 1.108 μs | 0.0026 μs | 0.0017 μs |  0.98 | 0.0038 |     120 B |        1.00 |
|                                |            |                |             |          |           |           |       |        |           |             |
| Generate_TimestampRandomFormat | ShortRun   | 3              | 1           | 1.225 μs | 0.1047 μs | 0.0057 μs |  1.12 | 0.0057 |     151 B |        1.26 |
| Generate_ToString              | ShortRun   | 3              | 1           | 1.170 μs | 0.2339 μs | 0.0128 μs |  1.07 | 0.0076 |     216 B |        1.80 |
| Generate_UlidFormat            | ShortRun   | 3              | 1           | 1.097 μs | 0.0238 μs | 0.0013 μs |  1.00 | 0.0038 |     120 B |        1.00 |
| Generate_UuidV7Format          | ShortRun   | 3              | 1           | 1.122 μs | 0.0473 μs | 0.0026 μs |  1.02 | 0.0038 |      96 B |        0.80 |
| ExtractShardId_Ulid            | ShortRun   | 3              | 1           | 1.110 μs | 0.1408 μs | 0.0077 μs |  1.01 | 0.0038 |     120 B |        1.00 |
