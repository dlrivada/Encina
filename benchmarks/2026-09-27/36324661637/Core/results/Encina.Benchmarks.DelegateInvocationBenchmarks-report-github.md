```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     40.05 ns |     2.861 ns |   0.157 ns |     1.26 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     31.76 ns |     5.152 ns |   0.282 ns |     1.00 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 92,400.19 ns | 5,708.143 ns | 312.883 ns | 2,909.90 |   23.90 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    140.62 ns |    12.437 ns |   0.682 ns |     4.43 |    0.04 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     81.47 ns |     9.409 ns |   0.516 ns |     2.57 |    0.02 | 0.0105 |      - |     176 B |        1.57 |
