```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error         | StdDev       | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|--------------:|-------------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     37.91 ns |     12.256 ns |     0.672 ns |     1.29 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     29.39 ns |      6.084 ns |     0.334 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 89,089.27 ns | 21,865.328 ns | 1,198.512 ns | 3,032.05 |   46.33 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    141.37 ns |     11.052 ns |     0.606 ns |     4.81 |    0.05 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     80.72 ns |     10.786 ns |     0.591 ns |     2.75 |    0.03 | 0.0105 |      - |     176 B |        1.57 |
