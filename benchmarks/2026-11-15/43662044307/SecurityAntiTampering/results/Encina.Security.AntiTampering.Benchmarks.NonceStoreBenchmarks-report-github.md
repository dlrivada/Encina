```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method      | Job        | IterationCount | LaunchCount | WarmupCount | Mean            | Error         | StdDev        | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |----------- |--------------- |------------ |------------ |----------------:|--------------:|--------------:|---------:|--------:|-------:|-------:|----------:|------------:|
| Add         | Job-NUBXJZ | 20             | Default     | 5           |     1,403.64 ns |     85.877 ns |     88.189 ns |     1.00 |    0.09 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | Job-NUBXJZ | 20             | Default     | 5           |        47.47 ns |      0.166 ns |      0.171 ns |     0.03 |    0.00 |      - |      - |         - |        0.00 |
| Exists_Miss | Job-NUBXJZ | 20             | Default     | 5           |        20.04 ns |      0.121 ns |      0.125 ns |     0.01 |    0.00 |      - |      - |         - |        0.00 |
| Cleanup     | Job-NUBXJZ | 20             | Default     | 5           | 5,322,169.22 ns | 24,244.046 ns | 25,940.857 ns | 3,806.38 |  242.62 | 7.8125 |      - |  209640 B |    1,455.83 |
|             |            |                |             |             |                 |               |               |          |         |        |        |           |             |
| Add         | MediumRun  | 15             | 2           | 10          |     1,427.55 ns |     45.480 ns |     60.714 ns |     1.00 |    0.06 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | MediumRun  | 15             | 2           | 10          |        48.84 ns |      1.288 ns |      1.847 ns |     0.03 |    0.00 |      - |      - |         - |        0.00 |
| Exists_Miss | MediumRun  | 15             | 2           | 10          |        20.05 ns |      0.040 ns |      0.059 ns |     0.01 |    0.00 |      - |      - |         - |        0.00 |
| Cleanup     | MediumRun  | 15             | 2           | 10          | 5,361,965.13 ns | 17,477.249 ns | 26,159.126 ns | 3,762.78 |  162.35 | 7.8125 |      - |  209640 B |    1,455.83 |
