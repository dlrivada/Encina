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
| CompiledDelegate        |     38.43 ns |     1.860 ns |   0.102 ns |     1.30 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     29.57 ns |    10.886 ns |   0.597 ns |     1.00 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 90,582.75 ns | 4,768.927 ns | 261.401 ns | 3,063.88 |   54.66 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    141.57 ns |    21.533 ns |   1.180 ns |     4.79 |    0.09 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     83.08 ns |    41.603 ns |   2.280 ns |     2.81 |    0.08 | 0.0105 |      - |     176 B |        1.57 |
