```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.202
  [Host]     : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v4


```
| Method      | Job        | IterationCount | LaunchCount | WarmupCount | Mean            | Error         | StdDev        | Median          | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |----------- |--------------- |------------ |------------ |----------------:|--------------:|--------------:|----------------:|---------:|--------:|-------:|-------:|----------:|------------:|
| Add         | Job-NUBXJZ | 20             | Default     | 5           |     1,259.31 ns |     67.851 ns |     69.678 ns |     1,272.84 ns |     1.00 |    0.08 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | Job-NUBXJZ | 20             | Default     | 5           |        46.46 ns |      0.073 ns |      0.084 ns |        46.42 ns |     0.04 |    0.00 |      - |      - |         - |        0.00 |
| Exists_Miss | Job-NUBXJZ | 20             | Default     | 5           |        18.07 ns |      0.017 ns |      0.019 ns |        18.07 ns |     0.01 |    0.00 |      - |      - |         - |        0.00 |
| Cleanup     | Job-NUBXJZ | 20             | Default     | 5           | 5,361,608.36 ns |  9,461.979 ns | 10,516.964 ns | 5,360,900.93 ns | 4,270.25 |  236.89 | 7.8125 |      - |  209656 B |    1,455.94 |
|             |            |                |             |             |                 |               |               |                 |          |         |        |        |           |             |
| Add         | MediumRun  | 15             | 2           | 10          |     1,279.96 ns |     38.859 ns |     50.528 ns |     1,295.55 ns |     1.00 |    0.06 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | MediumRun  | 15             | 2           | 10          |        47.20 ns |      0.515 ns |      0.705 ns |        47.27 ns |     0.04 |    0.00 |      - |      - |         - |        0.00 |
| Exists_Miss | MediumRun  | 15             | 2           | 10          |        18.12 ns |      0.036 ns |      0.053 ns |        18.15 ns |     0.01 |    0.00 |      - |      - |         - |        0.00 |
| Cleanup     | MediumRun  | 15             | 2           | 10          | 5,345,640.11 ns | 14,628.712 ns | 21,442.578 ns | 5,341,295.02 ns | 4,182.96 |  170.21 | 7.8125 |      - |  209656 B |    1,455.94 |
