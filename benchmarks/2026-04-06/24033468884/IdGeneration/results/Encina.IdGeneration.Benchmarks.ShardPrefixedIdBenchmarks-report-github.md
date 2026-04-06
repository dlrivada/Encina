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
| Generate_UlidFormat            | Job-YFEFPZ | 10             | Default     | 3           | 1.246 μs | 0.0029 μs | 0.0019 μs | 1.246 μs |  1.00 |    0.00 | 0.0057 |     120 B |        1.00 |
| Generate_UuidV7Format          | Job-YFEFPZ | 10             | Default     | 3           | 1.205 μs | 0.0050 μs | 0.0030 μs | 1.205 μs |  0.97 |    0.00 | 0.0057 |      96 B |        0.80 |
| Generate_TimestampRandomFormat | Job-YFEFPZ | 10             | Default     | 3           | 1.434 μs | 0.0041 μs | 0.0021 μs | 1.435 μs |  1.15 |    0.00 | 0.0076 |     151 B |        1.26 |
| ExtractShardId_Ulid            | Job-YFEFPZ | 10             | Default     | 3           | 1.260 μs | 0.0072 μs | 0.0043 μs | 1.259 μs |  1.01 |    0.00 | 0.0057 |     120 B |        1.00 |
| Generate_ToString              | Job-YFEFPZ | 10             | Default     | 3           | 1.431 μs | 0.0035 μs | 0.0023 μs | 1.431 μs |  1.15 |    0.00 | 0.0114 |     216 B |        1.80 |
|                                |            |                |             |             |          |           |           |          |       |         |        |           |             |
| Generate_UlidFormat            | MediumRun  | 15             | 2           | 10          | 1.289 μs | 0.0250 μs | 0.0350 μs | 1.318 μs |  1.00 |    0.04 | 0.0057 |     120 B |        1.00 |
| Generate_UuidV7Format          | MediumRun  | 15             | 2           | 10          | 1.200 μs | 0.0029 μs | 0.0041 μs | 1.200 μs |  0.93 |    0.03 | 0.0057 |      96 B |        0.80 |
| Generate_TimestampRandomFormat | MediumRun  | 15             | 2           | 10          | 1.388 μs | 0.0281 μs | 0.0420 μs | 1.391 μs |  1.08 |    0.04 | 0.0076 |     151 B |        1.26 |
| ExtractShardId_Ulid            | MediumRun  | 15             | 2           | 10          | 1.291 μs | 0.0108 μs | 0.0148 μs | 1.280 μs |  1.00 |    0.03 | 0.0057 |     120 B |        1.00 |
| Generate_ToString              | MediumRun  | 15             | 2           | 10          | 1.337 μs | 0.0027 μs | 0.0040 μs | 1.337 μs |  1.04 |    0.03 | 0.0114 |     216 B |        1.80 |
