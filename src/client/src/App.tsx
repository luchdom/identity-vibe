import { useState } from 'react'

function App() {
  const [count, setCount] = useState(0)

  return (
    <div className="min-h-screen bg-gray-100 flex items-center justify-center">
      <div className="bg-white p-8 rounded-lg shadow-lg max-w-md w-full">
        <h1 className="text-3xl font-bold text-center text-gray-800 mb-6">
          Tailwind CSS Test
        </h1>
        <div className="space-y-4">
          <button 
            onClick={() => setCount((count) => count + 1)}
            className="w-full bg-blue-500 hover:bg-blue-600 text-white font-medium py-3 px-4 rounded-lg transition-colors duration-200"
          >
            Count is {count}
          </button>
          <div className="bg-green-50 border border-green-200 rounded-lg p-4">
            <p className="text-green-800 text-sm">
              ✅ Tailwind CSS is working! Try clicking the button above.
            </p>
          </div>
          <div className="grid grid-cols-3 gap-2">
            <div className="h-12 bg-red-300 rounded"></div>
            <div className="h-12 bg-green-300 rounded"></div>
            <div className="h-12 bg-blue-300 rounded"></div>
          </div>
        </div>
      </div>
    </div>
  )
}

export default App
