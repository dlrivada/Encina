```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |---------:|----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Generate_TimestampRandomFormat | Job-YFEFPZ | 10             | Default     | 3           | 1.395 μs | 0.0038 μs | 0.0023 μs | 1.394 μs |  1.09 |    0.00 | 0.0076 |     151 B |        1.26 |
| Generate_ToString              | Job-YFEFPZ | 10             | Default     | 3           | 1.433 μs | 0.0026 μs | 0.0017 μs | 1.433 μs |  1.12 |    0.00 | 0.0114 |     216 B |        1.80 |
| Generate_UlidFormat            | Job-YFEFPZ | 10             | Default     | 3           | 1.279 μs | 0.0034 μs | 0.0020 μs | 1.279 μs |  1.00 |    0.00 | 0.0057 |     120 B |        1.00 |
| Generate_UuidV7Format          | Job-YFEFPZ | 10             | Default     | 3           | 1.317 μs | 0.0048 μs | 0.0032 μs | 1.317 μs |  1.03 |    0.00 | 0.0057 |      96 B |        0.80 |
| ExtractShardId_Ulid            | Job-YFEFPZ | 10             | Default     | 3           | 1.295 μs | 0.0058 μs | 0.0034 μs | 1.296 μs |  1.01 |    0.00 | 0.0057 |     120 B |        1.00 |
|                                |            |                |             |             |          |           |           |          |       |         |        |           |             |
| Generate_TimestampRandomFormat | MediumRun  | 15             | 2           | 10          | 1.421 μs | 0.0254 μs | 0.0372 μs | 1.453 μs |  1.09 |    0.05 | 0.0076 |     151 B |        1.26 |
| Generate_ToString              | MediumRun  | 15             | 2           | 10          | 1.405 μs | 0.0219 μs | 0.0328 μs | 1.406 μs |  1.08 |    0.04 | 0.0114 |     216 B |        1.80 |
| Generate_UlidFormat            | MediumRun  | 15             | 2           | 10          | 1.305 μs | 0.0290 μs | 0.0434 μs | 1.306 μs |  1.00 |    0.05 | 0.0057 |     120 B |        1.00 |
| Generate_UuidV7Format          | MediumRun  | 15             | 2           | 10          | 1.269 μs | 0.0239 μs | 0.0343 μs | 1.297 μs |  0.97 |    0.04 | 0.0057 |      96 B |        0.80 |
| ExtractShardId_Ulid            | MediumRun  | 15             | 2           | 10          | 1.292 μs | 0.0038 μs | 0.0053 μs | 1.293 μs |  0.99 |    0.03 | 0.0057 |     120 B |        1.00 |
