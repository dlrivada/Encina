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
| CompiledDelegate        |     39.31 ns |     5.159 ns |   0.283 ns |     1.26 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     31.27 ns |    11.164 ns |   0.612 ns |     1.00 |    0.02 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 91,417.46 ns | 8,151.728 ns | 446.824 ns | 2,924.03 |   51.60 | 0.2441 | 0.1221 |    5275 B |       47.10 |
| GenericTypeConstruction |    141.12 ns |     3.717 ns |   0.204 ns |     4.51 |    0.08 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     81.43 ns |     1.967 ns |   0.108 ns |     2.60 |    0.04 | 0.0105 |      - |     176 B |        1.57 |
