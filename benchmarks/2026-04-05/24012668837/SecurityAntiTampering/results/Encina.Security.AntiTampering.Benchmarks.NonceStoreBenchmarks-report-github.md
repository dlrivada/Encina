```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method      | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean             | Error         | StdDev       | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |----------- |--------------- |------------ |------------ |------------- |------------ |-----------------:|--------------:|-------------:|---------:|--------:|-------:|-------:|----------:|------------:|
| Add         | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1,235.01 ns |    194.873 ns |   101.922 ns |     1.01 |    0.11 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         56.27 ns |      0.116 ns |     0.077 ns |     0.05 |    0.00 |      - |      - |         - |        0.00 |
| Exists_Miss | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         25.22 ns |      0.149 ns |     0.099 ns |     0.02 |    0.00 |      - |      - |         - |        0.00 |
| Cleanup     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |  5,402,436.62 ns | 18,247.059 ns | 9,543.564 ns | 4,399.22 |  325.17 | 7.8125 |      - |  209656 B |    1,455.94 |
|             |            |                |             |             |              |             |                  |               |              |          |         |        |        |           |             |
| Add         | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,289,717.00 ns |            NA |     0.000 ns |     1.00 |    0.00 |      - |      - |     144 B |        1.00 |
| Exists_Hit  | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,217,163.00 ns |            NA |     0.000 ns |     0.98 |    0.00 |      - |      - |         - |        0.00 |
| Exists_Miss | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,137,324.00 ns |            NA |     0.000 ns |     0.95 |    0.00 |      - |      - |         - |        0.00 |
| Cleanup     | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 12,943,980.00 ns |            NA |     0.000 ns |     3.93 |    0.00 |      - |      - |  209656 B |    1,455.94 |
