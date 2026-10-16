```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     40.67 ns |     1.393 ns |   0.076 ns |     1.23 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     33.04 ns |     4.900 ns |   0.269 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 92,252.94 ns | 6,363.829 ns | 348.823 ns | 2,792.01 |   21.71 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    138.21 ns |    11.301 ns |   0.619 ns |     4.18 |    0.03 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     86.68 ns |     8.911 ns |   0.488 ns |     2.62 |    0.02 | 0.0105 |      - |     176 B |        1.57 |
