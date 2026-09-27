```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                         | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| Generate_TimestampRandomFormat | Job-YFEFPZ | 10             | Default     | 1.227 μs | 0.0062 μs | 0.0037 μs |  1.13 | 0.0057 |     151 B |        1.26 |
| Generate_ToString              | Job-YFEFPZ | 10             | Default     | 1.161 μs | 0.0060 μs | 0.0035 μs |  1.07 | 0.0076 |     216 B |        1.80 |
| Generate_UlidFormat            | Job-YFEFPZ | 10             | Default     | 1.082 μs | 0.0034 μs | 0.0022 μs |  1.00 | 0.0038 |     120 B |        1.00 |
| Generate_UuidV7Format          | Job-YFEFPZ | 10             | Default     | 1.074 μs | 0.0139 μs | 0.0092 μs |  0.99 | 0.0038 |      96 B |        0.80 |
| ExtractShardId_Ulid            | Job-YFEFPZ | 10             | Default     | 1.137 μs | 0.0099 μs | 0.0065 μs |  1.05 | 0.0038 |     120 B |        1.00 |
|                                |            |                |             |          |           |           |       |        |           |             |
| Generate_TimestampRandomFormat | ShortRun   | 3              | 1           | 1.237 μs | 0.0453 μs | 0.0025 μs |  1.12 | 0.0057 |     151 B |        1.26 |
| Generate_ToString              | ShortRun   | 3              | 1           | 1.178 μs | 0.2524 μs | 0.0138 μs |  1.07 | 0.0076 |     216 B |        1.80 |
| Generate_UlidFormat            | ShortRun   | 3              | 1           | 1.105 μs | 0.1337 μs | 0.0073 μs |  1.00 | 0.0038 |     120 B |        1.00 |
| Generate_UuidV7Format          | ShortRun   | 3              | 1           | 1.109 μs | 0.0471 μs | 0.0026 μs |  1.00 | 0.0038 |      96 B |        0.80 |
| ExtractShardId_Ulid            | ShortRun   | 3              | 1           | 1.099 μs | 0.1397 μs | 0.0077 μs |  0.99 | 0.0038 |     120 B |        1.00 |
