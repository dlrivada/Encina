```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |---------:|----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Generate_TimestampRandomFormat | Job-YFEFPZ | 10             | Default     | 3           | 1.523 μs | 0.0026 μs | 0.0015 μs | 1.523 μs |  1.14 |    0.00 | 0.0076 |     151 B |        1.26 |
| Generate_ToString              | Job-YFEFPZ | 10             | Default     | 3           | 1.438 μs | 0.0055 μs | 0.0037 μs | 1.437 μs |  1.08 |    0.00 | 0.0114 |     216 B |        1.80 |
| Generate_UlidFormat            | Job-YFEFPZ | 10             | Default     | 3           | 1.332 μs | 0.0056 μs | 0.0033 μs | 1.330 μs |  1.00 |    0.00 | 0.0057 |     120 B |        1.00 |
| Generate_UuidV7Format          | Job-YFEFPZ | 10             | Default     | 3           | 1.278 μs | 0.0063 μs | 0.0042 μs | 1.278 μs |  0.96 |    0.00 | 0.0057 |      96 B |        0.80 |
| ExtractShardId_Ulid            | Job-YFEFPZ | 10             | Default     | 3           | 1.359 μs | 0.0053 μs | 0.0035 μs | 1.359 μs |  1.02 |    0.00 | 0.0057 |     120 B |        1.00 |
|                                |            |                |             |             |          |           |           |          |       |         |        |           |             |
| Generate_TimestampRandomFormat | MediumRun  | 15             | 2           | 10          | 1.456 μs | 0.0025 μs | 0.0037 μs | 1.456 μs |  1.08 |    0.01 | 0.0076 |     151 B |        1.26 |
| Generate_ToString              | MediumRun  | 15             | 2           | 10          | 1.453 μs | 0.0027 μs | 0.0041 μs | 1.452 μs |  1.08 |    0.01 | 0.0114 |     216 B |        1.80 |
| Generate_UlidFormat            | MediumRun  | 15             | 2           | 10          | 1.347 μs | 0.0047 μs | 0.0065 μs | 1.346 μs |  1.00 |    0.01 | 0.0057 |     120 B |        1.00 |
| Generate_UuidV7Format          | MediumRun  | 15             | 2           | 10          | 1.325 μs | 0.0228 μs | 0.0327 μs | 1.301 μs |  0.98 |    0.02 | 0.0057 |      96 B |        0.80 |
| ExtractShardId_Ulid            | MediumRun  | 15             | 2           | 10          | 1.365 μs | 0.0051 μs | 0.0075 μs | 1.363 μs |  1.01 |    0.01 | 0.0057 |     120 B |        1.00 |
