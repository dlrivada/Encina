```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     36.31 ns |     3.312 ns |   0.182 ns |     1.25 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     29.00 ns |     5.419 ns |   0.297 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 91,015.06 ns | 2,342.738 ns | 128.413 ns | 3,138.30 |   27.97 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    128.56 ns |    15.280 ns |   0.838 ns |     4.43 |    0.05 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     80.07 ns |    12.684 ns |   0.695 ns |     2.76 |    0.03 | 0.0105 |      - |     176 B |        1.57 |
