```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     20.37 ns |     12.82 ns |   0.703 ns |     1.28 |    0.06 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     15.89 ns |     12.68 ns |   0.695 ns |     1.00 |    0.05 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 17,791.73 ns | 15,466.99 ns | 847.798 ns | 1,120.93 |   62.41 | 0.3052 | 0.2747 |    5287 B |       47.21 |
| GenericTypeConstruction |     66.86 ns |     25.92 ns |   1.421 ns |     4.21 |    0.18 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     43.69 ns |     11.43 ns |   0.627 ns |     2.75 |    0.11 | 0.0105 |      - |     176 B |        1.57 |
