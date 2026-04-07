```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.59GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                         | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Generate_UlidFormat            | Job-YFEFPZ | 10             | Default     | 1.081 μs | 0.0031 μs | 0.0018 μs |  1.00 |    0.00 | 0.0038 |     120 B |        1.00 |
| Generate_UuidV7Format          | Job-YFEFPZ | 10             | Default     | 1.037 μs | 0.0018 μs | 0.0009 μs |  0.96 |    0.00 | 0.0038 |      96 B |        0.80 |
| Generate_TimestampRandomFormat | Job-YFEFPZ | 10             | Default     | 1.278 μs | 0.0031 μs | 0.0020 μs |  1.18 |    0.00 | 0.0057 |     151 B |        1.26 |
| ExtractShardId_Ulid            | Job-YFEFPZ | 10             | Default     | 1.090 μs | 0.0019 μs | 0.0013 μs |  1.01 |    0.00 | 0.0038 |     120 B |        1.00 |
| Generate_ToString              | Job-YFEFPZ | 10             | Default     | 1.163 μs | 0.0040 μs | 0.0026 μs |  1.08 |    0.00 | 0.0076 |     216 B |        1.80 |
|                                |            |                |             |          |           |           |       |         |        |           |             |
| Generate_UlidFormat            | ShortRun   | 3              | 1           | 1.073 μs | 0.0314 μs | 0.0017 μs |  1.00 |    0.00 | 0.0038 |     120 B |        1.00 |
| Generate_UuidV7Format          | ShortRun   | 3              | 1           | 1.033 μs | 0.0433 μs | 0.0024 μs |  0.96 |    0.00 | 0.0038 |      96 B |        0.80 |
| Generate_TimestampRandomFormat | ShortRun   | 3              | 1           | 1.238 μs | 0.0503 μs | 0.0028 μs |  1.15 |    0.00 | 0.0057 |     151 B |        1.26 |
| ExtractShardId_Ulid            | ShortRun   | 3              | 1           | 1.168 μs | 0.4053 μs | 0.0222 μs |  1.09 |    0.02 | 0.0038 |     120 B |        1.00 |
| Generate_ToString              | ShortRun   | 3              | 1           | 1.189 μs | 0.1440 μs | 0.0079 μs |  1.11 |    0.01 | 0.0076 |     216 B |        1.80 |
