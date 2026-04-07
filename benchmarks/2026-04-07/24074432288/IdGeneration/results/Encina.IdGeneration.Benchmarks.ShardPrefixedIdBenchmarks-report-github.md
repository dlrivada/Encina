```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |---------:|----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Generate_UlidFormat            | Job-YFEFPZ | 10             | Default     | 3           | 1.212 μs | 0.0158 μs | 0.0104 μs | 1.213 μs |  1.00 |    0.01 | 0.0057 |     120 B |        1.00 |
| Generate_UuidV7Format          | Job-YFEFPZ | 10             | Default     | 3           | 1.258 μs | 0.0153 μs | 0.0101 μs | 1.261 μs |  1.04 |    0.01 | 0.0057 |      96 B |        0.80 |
| Generate_TimestampRandomFormat | Job-YFEFPZ | 10             | Default     | 3           | 1.313 μs | 0.0160 μs | 0.0106 μs | 1.316 μs |  1.08 |    0.01 | 0.0076 |     151 B |        1.26 |
| ExtractShardId_Ulid            | Job-YFEFPZ | 10             | Default     | 3           | 1.308 μs | 0.0176 μs | 0.0116 μs | 1.308 μs |  1.08 |    0.01 | 0.0057 |     120 B |        1.00 |
| Generate_ToString              | Job-YFEFPZ | 10             | Default     | 3           | 1.326 μs | 0.0228 μs | 0.0151 μs | 1.329 μs |  1.09 |    0.01 | 0.0114 |     216 B |        1.80 |
|                                |            |                |             |             |          |           |           |          |       |         |        |           |             |
| Generate_UlidFormat            | MediumRun  | 15             | 2           | 10          | 1.236 μs | 0.0081 μs | 0.0121 μs | 1.236 μs |  1.00 |    0.01 | 0.0057 |     120 B |        1.00 |
| Generate_UuidV7Format          | MediumRun  | 15             | 2           | 10          | 1.209 μs | 0.0312 μs | 0.0458 μs | 1.225 μs |  0.98 |    0.04 | 0.0057 |      96 B |        0.80 |
| Generate_TimestampRandomFormat | MediumRun  | 15             | 2           | 10          | 1.327 μs | 0.0104 μs | 0.0155 μs | 1.324 μs |  1.07 |    0.02 | 0.0076 |     151 B |        1.26 |
| ExtractShardId_Ulid            | MediumRun  | 15             | 2           | 10          | 1.290 μs | 0.0342 μs | 0.0501 μs | 1.260 μs |  1.04 |    0.04 | 0.0057 |     120 B |        1.00 |
| Generate_ToString              | MediumRun  | 15             | 2           | 10          | 1.333 μs | 0.0030 μs | 0.0045 μs | 1.332 μs |  1.08 |    0.01 | 0.0114 |     216 B |        1.80 |
