```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error         | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|--------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     37.12 ns |     18.854 ns |   1.033 ns |     1.21 |    0.03 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     30.59 ns |      9.325 ns |   0.511 ns |     1.00 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 93,880.48 ns | 10,180.361 ns | 558.020 ns | 3,069.56 |   47.14 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    140.97 ns |     24.857 ns |   1.363 ns |     4.61 |    0.08 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     76.67 ns |     14.584 ns |   0.799 ns |     2.51 |    0.04 | 0.0105 |      - |     176 B |        1.57 |
