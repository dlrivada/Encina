```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     35.74 ns |     4.420 ns |   0.242 ns |     1.26 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     28.42 ns |     0.871 ns |   0.048 ns |     1.00 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 89,931.73 ns | 3,728.114 ns | 204.350 ns | 3,164.18 |    7.74 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    136.33 ns |     5.938 ns |   0.325 ns |     4.80 |    0.01 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     76.15 ns |     7.684 ns |   0.421 ns |     2.68 |    0.01 | 0.0105 |      - |     176 B |        1.57 |
