```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error         | StdDev       | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|--------------:|-------------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     36.45 ns |      2.871 ns |     0.157 ns |     1.23 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     29.63 ns |      5.369 ns |     0.294 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 92,833.36 ns | 18,605.032 ns | 1,019.805 ns | 3,133.16 |   40.28 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    131.43 ns |     10.430 ns |     0.572 ns |     4.44 |    0.04 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     82.12 ns |     10.012 ns |     0.549 ns |     2.77 |    0.03 | 0.0105 |      - |     176 B |        1.57 |
