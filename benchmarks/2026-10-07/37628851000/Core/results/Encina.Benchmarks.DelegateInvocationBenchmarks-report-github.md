```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error       | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     37.74 ns |    12.99 ns |   0.712 ns |     1.24 |    0.04 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     30.46 ns |    15.65 ns |   0.858 ns |     1.00 |    0.03 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 91,585.67 ns | 5,682.66 ns | 311.486 ns | 3,008.46 |   74.90 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    142.07 ns |    13.15 ns |   0.721 ns |     4.67 |    0.12 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     83.29 ns |    21.01 ns |   1.152 ns |     2.74 |    0.08 | 0.0105 |      - |     176 B |        1.57 |
