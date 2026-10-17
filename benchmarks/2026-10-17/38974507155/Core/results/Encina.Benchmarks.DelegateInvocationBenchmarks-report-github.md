```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.08GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     40.50 ns |    12.330 ns |   0.676 ns |     1.26 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     32.23 ns |     7.976 ns |   0.437 ns |     1.00 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 92,191.98 ns | 4,342.776 ns | 238.042 ns | 2,860.94 |   33.96 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    135.60 ns |     0.943 ns |   0.052 ns |     4.21 |    0.05 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     85.43 ns |     5.625 ns |   0.308 ns |     2.65 |    0.03 | 0.0105 |      - |     176 B |        1.57 |
