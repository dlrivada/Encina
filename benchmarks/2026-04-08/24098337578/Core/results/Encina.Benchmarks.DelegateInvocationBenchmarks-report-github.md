```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                  | Mean         | Error      | StdDev     | Median       | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-----------:|-----------:|-------------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     35.90 ns |   0.043 ns |   0.061 ns |     35.91 ns |     1.00 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     35.82 ns |   0.073 ns |   0.108 ns |     35.81 ns |     1.00 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 89,034.31 ns | 662.124 ns | 970.533 ns | 89,430.57 ns | 2,485.68 |   27.63 | 0.2441 | 0.1221 |    5283 B |       47.17 |
| GenericTypeConstruction |    128.94 ns |   2.808 ns |   4.027 ns |    132.33 ns |     3.60 |    0.11 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     79.45 ns |   0.241 ns |   0.353 ns |     79.29 ns |     2.22 |    0.01 | 0.0105 |      - |     176 B |        1.57 |
