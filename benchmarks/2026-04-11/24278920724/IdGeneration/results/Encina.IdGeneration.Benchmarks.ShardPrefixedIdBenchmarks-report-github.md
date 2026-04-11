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
| Generate_TimestampRandomFormat | Job-YFEFPZ | 10             | Default     | 3           | 1.356 μs | 0.0034 μs | 0.0023 μs | 1.356 μs |  1.10 |    0.00 | 0.0076 |     151 B |        1.26 |
| Generate_ToString              | Job-YFEFPZ | 10             | Default     | 3           | 1.324 μs | 0.0054 μs | 0.0036 μs | 1.325 μs |  1.08 |    0.00 | 0.0114 |     216 B |        1.80 |
| Generate_UlidFormat            | Job-YFEFPZ | 10             | Default     | 3           | 1.230 μs | 0.0024 μs | 0.0014 μs | 1.230 μs |  1.00 |    0.00 | 0.0057 |     120 B |        1.00 |
| Generate_UuidV7Format          | Job-YFEFPZ | 10             | Default     | 3           | 1.199 μs | 0.0014 μs | 0.0008 μs | 1.199 μs |  0.97 |    0.00 | 0.0057 |      96 B |        0.80 |
| ExtractShardId_Ulid            | Job-YFEFPZ | 10             | Default     | 3           | 1.271 μs | 0.0067 μs | 0.0040 μs | 1.269 μs |  1.03 |    0.00 | 0.0057 |     120 B |        1.00 |
|                                |            |                |             |             |          |           |           |          |       |         |        |           |             |
| Generate_TimestampRandomFormat | MediumRun  | 15             | 2           | 10          | 1.377 μs | 0.0119 μs | 0.0174 μs | 1.366 μs |  1.08 |    0.04 | 0.0076 |     151 B |        1.26 |
| Generate_ToString              | MediumRun  | 15             | 2           | 10          | 1.330 μs | 0.0023 μs | 0.0032 μs | 1.330 μs |  1.04 |    0.03 | 0.0114 |     216 B |        1.80 |
| Generate_UlidFormat            | MediumRun  | 15             | 2           | 10          | 1.278 μs | 0.0285 μs | 0.0409 μs | 1.280 μs |  1.00 |    0.04 | 0.0057 |     120 B |        1.00 |
| Generate_UuidV7Format          | MediumRun  | 15             | 2           | 10          | 1.209 μs | 0.0090 μs | 0.0129 μs | 1.215 μs |  0.95 |    0.03 | 0.0057 |      96 B |        0.80 |
| ExtractShardId_Ulid            | MediumRun  | 15             | 2           | 10          | 1.305 μs | 0.0298 μs | 0.0446 μs | 1.308 μs |  1.02 |    0.05 | 0.0057 |     120 B |        1.00 |
