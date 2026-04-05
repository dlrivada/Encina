```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                         | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean          | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |--------------:|----------:|----------:|------:|-------:|----------:|------------:|
| Generate_UlidFormat            | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.224 μs | 0.0033 μs | 0.0022 μs |  1.00 | 0.0057 |     120 B |        1.00 |
| Generate_UuidV7Format          | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.206 μs | 0.0075 μs | 0.0050 μs |  0.98 | 0.0057 |      96 B |        0.80 |
| Generate_TimestampRandomFormat | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.359 μs | 0.0049 μs | 0.0033 μs |  1.11 | 0.0076 |     151 B |        1.26 |
| ExtractShardId_Ulid            | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.339 μs | 0.0027 μs | 0.0016 μs |  1.09 | 0.0057 |     120 B |        1.00 |
| Generate_ToString              | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.353 μs | 0.0062 μs | 0.0037 μs |  1.11 | 0.0114 |     216 B |        1.80 |
|                                |            |                |             |             |              |             |               |           |           |       |        |           |             |
| Generate_UlidFormat            | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 22,779.545 μs |        NA | 0.0000 μs |  1.00 |      - |     120 B |        1.00 |
| Generate_UuidV7Format          | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 20,646.899 μs |        NA | 0.0000 μs |  0.91 |      - |      96 B |        0.80 |
| Generate_TimestampRandomFormat | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 21,765.805 μs |        NA | 0.0000 μs |  0.96 |      - |     488 B |        4.07 |
| ExtractShardId_Ulid            | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 25,575.524 μs |        NA | 0.0000 μs |  1.12 |      - |     120 B |        1.00 |
| Generate_ToString              | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 22,927.011 μs |        NA | 0.0000 μs |  1.01 |      - |     216 B |        1.80 |
